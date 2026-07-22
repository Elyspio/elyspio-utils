using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Elyspio.Utils.Telemetry.Technical.Data;
using Elyspio.Utils.Telemetry.Technical.Constants;
using Elyspio.Utils.Telemetry.Technical.Extensions;
using Elyspio.Utils.Telemetry.Technical.Helpers;
using Elyspio.Utils.Telemetry.Technical.Options;
using Elyspio.Utils.Telemetry.Tracing.Elements.Base;
using Elyspio.Utils.Telemetry.Tracing.Samplers;
using Elyspio.Utils.Telemetry.Tracing.Markers;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Log = Serilog.Log;

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
public sealed class AppOpenTelemetryBuilder<TAssembly> : AppOpenTelemetryBuilder
{
	/// <summary>
	///    Constructeur qui ajoute automatiquement l'assembly de TAssembly
	/// </summary>
	/// <param name="options"></param>
	/// <param name="configuration"></param>
	public AppOpenTelemetryBuilder(AppOpenTelemetryBuilderOptions options, IConfiguration configuration) : base(options, configuration)
	{
		AddAssembly<TAssembly>();
	}
}

/// <summary>
///     Builder permettant de configurer OpenTelemetry
/// </summary>
/// <remarks>
///		Attention, il faut appeler <see cref="AddAssembly{T}"/> pour ajouter les classes à tracer
/// </remarks>
public class AppOpenTelemetryBuilder
{
	private readonly AppOpenTelemetryBuilderOptions _options;
	private readonly IConfiguration _configuration;

	/// <summary>
	/// Constructeur
	/// </summary>
	public AppOpenTelemetryBuilder(AppOpenTelemetryBuilderOptions options, IConfiguration configuration)
	{
		_options = options;
		_configuration = configuration;
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
	public Action<TracerProviderBuilder, AppOpenTelemetryBuilder>? Tracing { get; set; }

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


	private readonly HashSet<string> _sources = [];
	private readonly List<Sampler> _samplers = [];


	private readonly List<BaseProcessor<Activity>> _processors = [];


	/// <summary>
	/// Permet d'ajouter la télémétrie à une assembly (gestion des sous projets nuget)
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public AppOpenTelemetryBuilder AddAssembly<T>()
	{
		var cls = AssemblyHelper.GetClassWithInterface<T, ITracingController>()
			.Concat(AssemblyHelper.GetClassWithInterface<T, ITracingAttribute>())
			.Concat(AssemblyHelper.GetClassWithInterface<T, ITracingMiddleware>())
			.Concat(AssemblyHelper.GetClassWithInterface<T, ITracingService>())
			.Concat(AssemblyHelper.GetClassWithInterface<T, ITracingAdapter>())
			.Concat(AssemblyHelper.GetClassWithInterface<T, ITracingRepository>());

		foreach (var source in cls)
		{
			_sources.Add(source);
		}

		return this;
	}


	/// <summary>
	///     Active le tracing dans les services de l'application
	/// </summary>
	/// <param name="services"></param>
	/// <returns></returns>
	public IOpenTelemetryBuilder Build(IServiceCollection services)
	{
		if (_options.Debug == true)
		{
			Log.Logger.Information("Le débug est activé pour la télémétrie");
			services.AddOpenTelemetryEventLogging();
		}


		services.AddSingleton(_options.ShouldCapture);

		var (resourceBuilder, serviceName, instanceId) = GetResourceBuilder();

		Log.Logger.Information("La télémétrie est configurée {Service}@{InstanceId}", serviceName, instanceId);

		var builder = services.AddOpenTelemetry()
			.WithTracing(tracing =>
			{
				tracing.SetResourceBuilder(resourceBuilder);

				tracing.AddSource(_sources.ToArray());

				tracing.AddOtlpExporter(o => ConfigureOtlpExporter(o, BuilderType.Tracing));

				tracing.SetErrorStatusOnException();
				tracing.SetSampler(new MultiSampler(_samplers));


				foreach (var processor in _processors)
				{
					tracing.AddProcessor(processor);
				}

				InstrumentDotnetCore(tracing);

				Tracing?.Invoke(tracing, this);
			})
			.WithMetrics(metric =>
			{
				metric.SetResourceBuilder(resourceBuilder);

				metric.AddOtlpExporter(o => ConfigureOtlpExporter(o, BuilderType.Meter))
					.AddProcessInstrumentation()
					.AddRuntimeInstrumentation()
					.AddHttpClientInstrumentation()
					.AddAspNetCoreInstrumentation();


				metric.AddMeter(MetterConstants.DefaultMetters.Concat(_sources).Concat(Metters).ToArray());


				metric.AddView("request-duration",
					new ExplicitBucketHistogramConfiguration
					{
						Boundaries = [0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]
					}
				);


				Meter?.Invoke(metric);
			});

		if (TelemetryExtension.UseStandardConfiguration)
		{
			builder.WithLogging(b => b.AddOtlpExporter(o => ConfigureOtlpExporter(o, BuilderType.Logging)));
		}

		return builder;
	}

	/// <summary>
	///    Active les instrumentations par défaut pour les applications .NET Core (ASP.NET Core et HttpClient)
	/// </summary>
	/// <param name="tracing"></param>
	private void InstrumentDotnetCore(TracerProviderBuilder tracing)
	{
		tracing.AddHttpClientInstrumentation(o =>
		{
			o.RecordException = true;
			o.EnrichWithException = (activity, exception) => { activity.SetTag("exception", exception); };
			o.EnrichWithHttpRequestMessage = (activity, message) =>
			{
				if (!_options.Cache.Activated || !_options.Cache.Levels.TryGetValue(Technical.Options.Capture.CaptureLevel.Trace, out var enabled) || !enabled) return;
				try
				{
					activity.SetTag("http.request.headers", JsonConvert.SerializeObject(message.Headers));
					if (message.Content is null) return;
					if (message.Content.Headers.ContentLength > 32 * 1024) { activity.SetTag("http.request.content", "[Payload too large to record]"); return; }
					activity.SetTag("http.request.content", message.Content.ReadAsStringAsync().GetAwaiter().GetResult());
				}
				catch (Exception) { }
			};
			o.EnrichWithHttpResponseMessage = (activity, message) =>
			{
				if (_options.Cache.Activated && _options.Cache.Levels.TryGetValue(Technical.Options.Capture.CaptureLevel.Trace, out var enabled) && enabled)
					activity.SetTag("http.response.headers", JsonConvert.SerializeObject(message.Headers));
			};

			HttpClientInstrumentation?.Invoke(o);
		});

		tracing.AddAspNetCoreInstrumentation(o =>
		{
			o.RecordException = true;
			o.Filter = ctx => { return IgnorePaths.All(p => !ctx.Request.Path.StartsWithSegments(p)); };
			o.EnrichWithHttpResponse = HttpHelper.EnrichWithHttpResponse;
			o.EnrichWithException = (activity, exception) => { activity.SetTag("exception", exception); };

			AspNetCoreInstrumentation?.Invoke(o);
		});
	}



	/// <summary>
	///    Ajoute un sampler à la liste des samplers à utiliser
	/// </summary>
	/// <param name="sampler"></param>
	[PublicAPI]
	public void AddSampler(Sampler sampler)
	{
		_samplers.Add(sampler);
	}

	/// <summary>
	///   Ajoute un sampler à la liste des samplers à utiliser
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[PublicAPI]
	public void AddSampler<T>() where T : Sampler, new()
	{
		_samplers.Add(new T());
	}
	
	/// <summary>
	///    Ajoute un processor à la liste des samplers à utiliser
	/// </summary>
	/// <param name="processor"></param>
	[PublicAPI]
	public void AddProcessor<T>(T processor) where T : BaseProcessor<Activity>
	{
		_processors.Add(processor);
	}
	
	private void ConfigureOtlpExporter(OtlpExporterOptions o, BuilderType type)
	{
		if (TelemetryExtension.UseStandardConfiguration) return;

		o.Protocol = _options.Protocol;

		var endpointUrl = _options.CollectorUri.ToString();

		if (o.Protocol == OtlpExportProtocol.HttpProtobuf)
		{
			endpointUrl += $"v1/{(type == BuilderType.Meter ? "metrics" : "traces")}";
		}

		// Lors des TU, on utilise le ExportProcessorType.Simple pour éviter le batching qui n'a pas forcément le temps de s'exécuter avant la fin du test
		if (_options.RunningInTestEnvironment == true)
		{
			o.ExportProcessorType = ExportProcessorType.Simple;
		}

		o.Endpoint = new Uri(endpointUrl);

		if (_options.Authentication is not null)
			o.HttpClientFactory = () => CreateMtlsClient(_options.Authentication);

	}

	private static HttpClient CreateMtlsClient(Technical.Options.Auth.CertificateAuthenticationOptions authentication)
	{
		var certificate = X509Certificate2.CreateFromPemFile(authentication.CertificatePemPath, authentication.CertificateKeyPath);
		if (OperatingSystem.IsWindows())
		{
			const string password = "elyspio-telemetry";
			certificate = X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx, password), password);
		}
		var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, cert, chain, _) => ValidateCertificate(cert, chain, authentication.CaPemPath) };
		handler.ClientCertificates.Add(certificate);
		return new HttpClient(handler);
	}

	private static bool ValidateCertificate(X509Certificate2? certificate, X509Chain? chain, string caPath)
	{
		if (certificate is null || chain is null) return false;
		chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
		chain.ChainPolicy.CustomTrustStore.Add(X509CertificateLoader.LoadCertificateFromFile(caPath));
		return chain.Build(certificate);
	}

	private (ResourceBuilder builder, string serviceName, string? serviceInstanceId) GetResourceBuilder()
	{
		var name = _configuration.GetValue<string>("OTEL_SERVICE_NAME ") ?? _options.Service;

		var version = _configuration.GetValue<string>("OTEL_SERVICE_VERSION") ?? _options.Version;

		var serviceInstanceId = GetServiceInstanceId();

		var builder = ResourceBuilder.CreateDefault().AddService(name, serviceVersion: version, serviceInstanceId: string.IsNullOrWhiteSpace(serviceInstanceId) ? null : serviceInstanceId);

		return (builder, name, serviceInstanceId);
	}




	/// <summary>
	/// Récupère le hostname de la machine à partir de la variable "OTEL_SERVICE_INSTANCE_ID" ou "ALIASSARA" pour gérer le multi branche en SARA
	/// </summary>
	/// <returns></returns>
	private string? GetServiceInstanceId() => Environment.GetEnvironmentVariable("OTEL_SERVICE_INSTANCE_ID");
}
