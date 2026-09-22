import { spawnSync } from "node:child_process";
import * as path from "node:path";
import * as fs from "node:fs";

export type GenerateHeyApiParams = {
	/** OpenAPI document, as a URL or a file path. */
	input: string;
	/** Output folder, emptied on each run. */
	output: string;
	/** HTTP client used by the generated SDK. */
	client?: "fetch" | "axios";
	/** Also generate TanStack Query options, query keys and mutations. */
	tanstackQuery?: boolean;
};

/**
 * Generate a typed SDK with `@hey-api/openapi-ts` (optional peer dependency), with TanStack Query helpers by default.
 */
export async function generateHeyApi({ input, output, client = "fetch", tanstackQuery = true }: GenerateHeyApiParams) {
	let heyApi: typeof import("@hey-api/openapi-ts");
	try {
		heyApi = await import("@hey-api/openapi-ts");
	} catch (error) {
		if ((error as NodeJS.ErrnoException).code === "ERR_MODULE_NOT_FOUND") {
			throw new Error("generateHeyApi requires @hey-api/openapi-ts: pnpm add -D @hey-api/openapi-ts", { cause: error });
		}
		throw error;
	}

	await heyApi.createClient({
		input,
		output,
		plugins: [`@hey-api/client-${client}`, "@hey-api/typescript", "@hey-api/sdk", ...(tanstackQuery ? (["@tanstack/react-query"] as const) : [])],
	});
}

/**
 * Generate a `typescript-axios` client with OpenAPI Generator (requires Java).
 * Kept for existing projects; prefer {@link generateHeyApi} for new ones.
 */
export async function generateApi(url: string, outputFolder: string, tag: string) {
	if (fs.existsSync(outputFolder)) {
		await fs.promises.rm(outputFolder, { recursive: true, force: true });
	}
	await fs.promises.mkdir(outputFolder, { recursive: true });

	const result = spawnSync(
		"npx",
		[
			"@openapitools/openapi-generator-cli",
			"generate",
			"-i",
			url,
			"-g",
			"typescript-axios",
			"-o",
			".",
			"--additional-properties=enumsAsTypes=true,supportsES6=true,withInterfaces=true",
			`--openapi-normalizer SET_TAGS_FOR_ALL_OPERATIONS=${tag}`,
		],
		{
			stdio: "inherit",
			shell: true,
			cwd: outputFolder,
			env: {
				...process.env,
				JAVA_OPTS: "-Dio.swagger.parser.util.RemoteUrl.trustAll=true -Dio.swagger.v3.parser.util.RemoteUrl.trustAll=true",
			},
		}
	);
	if (result.status !== 0) {
		throw new Error(`OpenAPI generator exited with code ${result.status ?? "unknown"}.`);
	}
	await cleanGeneratedFolder(outputFolder);
	await addTsIgnore(outputFolder);
}

async function cleanGeneratedFolder(folder: string) {
	const elementsToRemove = [".openapi-generator", ".gitignore", ".npmignore", "openapitools.json", ".openapi-generator-ignore", "git_push.sh"];
	await Promise.all(elementsToRemove.map((p) => fs.promises.rm(path.join(folder, p), { recursive: true, force: true })));
}

async function addTsIgnore(folder: string) {
	const files = await fs.promises.readdir(folder);

	for (const file of files.filter((fileName: string) => fileName.endsWith(".ts"))) {
		const filepath = path.join(folder, file);
		let content = (await fs.promises.readFile(filepath)).toString();
		content = "// @ts-nocheck\n" + content;
		await fs.promises.writeFile(filepath, content);
	}
}
