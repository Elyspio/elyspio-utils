import { spawnSync } from "child_process";
import * as path from "node:path";
import * as fs from "node:fs";

export async function generateApi(url: string, outputFolder: string, tag: string) {
	if (fs.existsSync(outputFolder)) {
		await fs.promises.rm(outputFolder, { recursive: true });
	}
	await fs.promises.mkdir(outputFolder, { recursive: true });

	spawnSync(
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
	await cleanGeneratedFolder(outputFolder);
	await addTsIgnore(outputFolder);
}

async function cleanGeneratedFolder(folder: string) {
	const elementsToRemove = [".openapi-generator", ".gitignore", ".npmignore", "openapitools.json", ".openapi-generator-ignore", "git_push.sh"];
	await Promise.all(elementsToRemove.map((p) => fs.promises.rm(path.join(folder, p), { recursive: true, force: true })));
}

async function addTsIgnore(folder: string) {
	const files = await fs.promises.readdir(folder);

	for (const file of files.filter((f) => f.endsWith(".ts"))) {
		const filepath = path.join(folder, file);
		let content = (await fs.promises.readFile(filepath)).toString();
		content = "// @ts-nocheck\n" + content;
		await fs.promises.writeFile(filepath, content);
	}
}
