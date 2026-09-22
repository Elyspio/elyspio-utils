import { execSync } from "node:child_process";
import * as fs from "node:fs/promises";
import * as path from "node:path";

const packageName = "@elyspio/vite-eslint-config";
const packageJsonPath = path.resolve(import.meta.dirname, "..", "package.json");

type Version = [major: number, minor: number, patch: number];

function parse(raw: string): Version {
	const match = /^(\d+)\.(\d+)\.(\d+)/.exec(raw.trim());
	if (!match) {
		throw new Error(`Unable to parse the version "${raw}" for ${packageName}.`);
	}
	return [Number(match[1]), Number(match[2]), Number(match[3])];
}

function compare(a: Version, b: Version) {
	return a[0] - b[0] || a[1] - b[1] || a[2] - b[2];
}

/**
 * Resolve the version to publish: an explicit argument wins, then a local version above the published one (manual major or minor bump),
 * otherwise the next patch of the published version.
 */
function resolveVersion(local: string, explicit?: string) {
	if (explicit) {
		return explicit;
	}

	const remote = parse(execSync(`npm show ${packageName} version`).toString());
	console.log("Remote version", remote.join("."));

	if (compare(parse(local), remote) > 0) {
		return local;
	}
	return [remote[0], remote[1], remote[2] + 1].join(".");
}

async function main(explicit?: string) {
	const json = JSON.parse(await fs.readFile(packageJsonPath, "utf8")) as { version: string };
	json.version = resolveVersion(json.version, explicit);
	console.log("New version", json.version);

	await fs.writeFile(packageJsonPath, `${JSON.stringify(json, null, "\t")}\n`);
}

await main(process.argv[2]);
