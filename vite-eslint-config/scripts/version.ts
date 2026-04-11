import semver, { inc } from "semver";
import * as path from "node:path";
import * as fs from "node:fs/promises";
import { execSync } from "node:child_process";
import { fileURLToPath } from "node:url";

const packageName = "@elyspio/vite-eslint-config";
const dirname = path.dirname(fileURLToPath(import.meta.url));

/**
 * Get latest version of the package from DevOps artifacts
 */
async function getPackageVersion() {
	const raw = execSync(`npm show ${packageName} version`).toString();

	return semver.parse(raw);
}

/**
 * Write the new version to the package.json
 * @param version
 */
async function writeVersionToPackageJson(version: string) {
	const packageJsonPath = path.resolve(dirname, "..", "package.json");

	let raw = (await fs.readFile(packageJsonPath)).toString();
	const json = JSON.parse(raw) as { version: string };
	json.version = version;

	raw = JSON.stringify(json, null, 4).replaceAll("    ", "\t");

	await fs.writeFile(packageJsonPath, raw);
}

async function main(version?: string) {
	if (!version) {
		const serverVersion = await getPackageVersion();
		if (!serverVersion) {
			throw new Error(`Unable to parse the published version for ${packageName}.`);
		}

		console.log("Remote version", serverVersion.raw);

		version = inc(serverVersion, "patch")!;
		console.log("New version", version);
	}

	await writeVersionToPackageJson(version);
}

void main(process.argv[2]);
// void main("5.0.0");
