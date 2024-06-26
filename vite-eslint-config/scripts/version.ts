/* eslint-disable import/no-extraneous-dependencies */
import semver, { inc } from "semver";
import * as path from "path";
import * as fs from "fs/promises";
import {execSync} from "node:child_process";

const packageName = "@elyspio/vite-eslint-config"

/**
 * Get latest version of the package from DevOps artifacts
 */
async function getPackageVersion() {

	const raw = execSync(`npm show ${packageName} version`).toString()

	return semver.parse(raw);
}

/**
 * Write the new version to the package.json
 * @param version
 */
async function writeVersionToPackageJson(version: string) {
	const packageJsonPath = path.resolve(__dirname, "..", "src", "package.json");

	let raw = (await fs.readFile(packageJsonPath)).toString();
	const json = JSON.parse(raw) as { version: string };
	json.version = version;

	raw = JSON.stringify(json, null, 4).replaceAll("    ", "\t");

	await fs.writeFile(packageJsonPath, raw);
}


async function main(version?: string) {
	if (!version) {
		const serverVersion = await getPackageVersion();
		console.log("Remote version", serverVersion.raw);

		version = inc(serverVersion, "patch")!;
		console.log("New version", version);
	}

	await writeVersionToPackageJson(version);
}

//
// // eslint-disable-next-line no-void
void main();
