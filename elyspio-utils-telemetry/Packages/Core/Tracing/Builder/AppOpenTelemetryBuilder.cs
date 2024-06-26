using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Elyspio.Utils.Telemetry.Technical.Constants;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Technical.Options;
using Elyspio.Utils.Telemetry.Technical.Options.Capture;
using Elyspio.Utils.Telemetry.Tracing.Elements.Base;
using Elyspio.Utils.Telemetry.Tracing.Markers;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Elyspio.Utils.Telemetry.Tracing.Builder;

/// <summary>
///     Builder permettant de configurer OpenTelemetry
/// </summary>
/// <typeparam name="TAssembly">
///     Marker dans l'application depuis lequel les classes implémentant
///     <see cref="ITracingController" />,
///     <see cref="ITracingService" />,
///     <see cref="ITracingAdapter" />,
///     <see cref="ITracingRepository" />,
///     vont être recherchées
/// </typeparam>
public sealed class AppOpenTelemetryBuilder<TAssembly>
{
	private readonly AppOpenTelemetryBuilderOptions _options;

	/// <param name="options"></param>
	public AppOpenTelemetryBuilder(AppOpenTelemetryBuilderOptions options)
	{
		_options = options;
		TracingContext.OpenTelemetryOptions = _options;
	}


    /// <summary>
    ///     Permet de configurer les options de l'instrumentation ASP.NET Core
    /// </summary>
    public Action<AspNetCoreTraceInstrumentationOptions>? AspNetCoreInstrumentation { get; set; }

    /// <summary>
    ///     Permet de configurer les options de l'instrumentation des clients HTTP
    /// </summary>
    public Action<HttpClientTraceInstrumentationOptions>? HttpClientInstrumentation { get; set; }

    /// <summary>
    ///     Configure le tracing
    /// </summary>
    public Action<TracerProviderBuilder>? Tracing { get; set; }

    /// <summary>
    ///     Configure les métriques
    /// </summary>
    public Action<MeterProviderBuilder>? Meter { get; set; }

    /// <summary>
    ///     Chemins à ignorer pour le tracing
    /// </summary>
    /// <example>
    ///     Par défaut : /swagger
    /// </example>
    public string[] IgnorePaths { get; set; } =
	[
		"/swagger"
	];


    /// <summary>
    ///     Métriques à ajouter
    /// </summary>
    public string[] Metters { get; set; } = [];


    /// <summary>
    ///     Active le tracing dans les services de l'application
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public IOpenTelemetryBuilder Build(IServiceCollection services)
	{
		if (_options.Debug == true) services.AddOpenTelemetryEventLogging();

		var resourceBuilder = ResourceBuilder.CreateEmpty().AddService(_options.Service, serviceVersion: _options.Version);

		var sources = new List<string>();

		// Rest
		sources.AddRange(AssemblyHelper.GetClassWithInterface<TAssembly, ITracingController>());
		sources.AddRange(AssemblyHelper.GetClassWithInterface<TAssembly, ITracingAttribute>());
		sources.AddRange(AssemblyHelper.GetClassWithInterface<TAssembly, ITracingMiddleware>());

		// Common
		sources.AddRange(AssemblyHelper.GetClassWithInterface<TAssembly, ITracingService>());
		sources.AddRange(AssemblyHelper.GetClassWithInterface<TAssembly, ITracingAdapter>());
		sources.AddRange(AssemblyHelper.GetClassWithInterface<TAssembly, ITracingRepository>());


		services.AddSingleton(_options.ShouldCapture);

		return services.AddOpenTelemetry()
			.WithTracing(tracing =>
			{
				tracing.SetResourceBuilder(resourceBuilder);

				tracing.AddSource(sources.ToArray());

				tracing.AddOtlpExporter(o => ConfigureOtlpExporter(o, BuilderType.Tracing));

				tracing.SetErrorStatusOnException();

				tracing.AddHttpClientInstrumentation(o =>
				{
					o.RecordException = true;
					o.EnrichWithException = (activity, exception) => { activity.SetTag("exception", exception); };

					o.EnrichWithHttpRequestMessage = (activity, message) =>
					{
						if (!_options.CaptureCache.Http[CaptureHttp.RequestBody]) return;
						activity.SetTag("http.request.content", message.Content?.ReadAsStringAsync().Result);
					};


					o.EnrichWithHttpResponseMessage = (activity, message) =>
					{
						if (!_options.CaptureCache.Http[CaptureHttp.ResponseBody]) return;
						activity.SetTag("http.response.content", message.Content?.ReadAsStringAsync().Result);
					};

					HttpClientInstrumentation?.Invoke(o);
				});

				tracing.AddAspNetCoreInstrumentation(o =>
				{
					o.RecordException = true;
					o.Filter = ctx => IgnorePaths.All(p => !ctx.Request.Path.StartsWithSegments(p));
					o.EnrichWithHttpResponse = HttpHelper.EnrichWithHttpResponse;
					o.EnrichWithException = (activity, exception) => { activity.SetTag("exception", exception); };

					AspNetCoreInstrumentation?.Invoke(o);
				});

				Tracing?.Invoke(tracing);
			})
			.WithMetrics(metric =>
			{
				metric.SetResourceBuilder(resourceBuilder);

				metric.AddOtlpExporter(o => ConfigureOtlpExporter(o, BuilderType.Meter))
					.AddProcessInstrumentation()
					.AddRuntimeInstrumentation()
					.AddHttpClientInstrumentation()
					.AddAspNetCoreInstrumentation();


				metric.AddMeter(MetterConstants.DefaultMetters.Concat(sources).Concat(Metters).ToArray());


				metric.AddView("request-duration",
					new ExplicitBucketHistogramConfiguration
					{
						Boundaries = [0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]
					}
				);

				Meter?.Invoke(metric);
			});
	}

	private void ConfigureOtlpExporter(OtlpExporterOptions o, BuilderType type)
	{
		o.Protocol = _options.Protocol;

		var endpointUrl = _options.CollectorUri.ToString();
		if (o.Protocol == OtlpExportProtocol.HttpProtobuf) endpointUrl += $"v1/{(type == BuilderType.Meter ? "metrics" : "traces")}";
		o.Endpoint = new Uri(endpointUrl);

		if (_options.Authentication is not null)
		{
			o.HttpClientFactory = () =>
			{
				var certificate = X509Certificate2.CreateFromPemFile(_options.Authentication.CertificatePemPath, _options.Authentication.CertificateKeyPath);

				// Windows ne gère pas les certificats PEM donc on les convertit en PFX
				if (Environment.OSVersion.Platform == PlatformID.Win32NT)
				{
					certificate = new X509Certificate2(certificate.Export(X509ContentType.Pfx));
				}

				var handler = new HttpClientHandler
				{
					ClientCertificates =
					{
						certificate
					},
					ServerCertificateCustomValidationCallback = ValidateCertificate,
				};
				var client = new HttpClient(handler);
				return client;
			};
		}
	}

	private bool ValidateCertificate(HttpRequestMessage message, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors arg4)
	{
		if (cert == null) return false;
		if (chain == null) return false;

		chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
		chain.ChainPolicy.CustomTrustStore.Add(new X509Certificate2(_options.Authentication!.CaPemPath));

		return chain.Build(cert);
	}

	private enum BuilderType
	{
		Tracing,
		Meter,
		Logging
	}
}