import { spawnSync } from "node:child_process";
import * as path from "node:path";
import * as fs from "node:fs";

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
		},
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
