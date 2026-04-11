import * as fs from "node:fs/promises";
import * as path from "node:path";
import { fileURLToPath } from "node:url";

type PackageJson = {
	author?: string | { email?: string; name?: string; url?: string };
	bugs?: string | { email?: string; url?: string };
	dependencies?: Record<string, string>;
	description?: string;
	engines?: Record<string, string>;
	exports?: Record<string, unknown>;
	files?: string[];
	funding?: string | { url?: string };
	homepage?: string;
	keywords?: string[];
	license?: string;
	name: string;
	peerDependencies?: Record<string, string>;
	publishConfig?: Record<string, unknown>;
	repository?: string | { directory?: string; type?: string; url?: string };
	sideEffects?: boolean | string[];
	type?: string;
	types?: string;
	version: string;
};

const dirname = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(dirname, "..");
const distDir = path.join(rootDir, "dist");

async function copyIfPresent(from: string, to = path.basename(from)) {
	const source = path.join(rootDir, from);
	const target = path.join(distDir, to);

	try {
		await fs.copyFile(source, target);
	} catch (error) {
		const nodeError = error as NodeJS.ErrnoException;
		if (nodeError.code !== "ENOENT") {
			throw error;
		}
	}
}

async function createDistPackageJson() {
	const raw = await fs.readFile(path.join(rootDir, "package.json"), "utf8");
	const packageJson = JSON.parse(raw) as PackageJson;

	const publishPackageJson = {
		name: packageJson.name,
		version: packageJson.version,
		description: packageJson.description,
		type: packageJson.type,
		license: packageJson.license,
		types: packageJson.types,
		exports: packageJson.exports,
		files: packageJson.files,
		sideEffects: packageJson.sideEffects,
		dependencies: packageJson.dependencies,
		peerDependencies: packageJson.peerDependencies,
		repository: packageJson.repository,
		homepage: packageJson.homepage,
		bugs: packageJson.bugs,
		author: packageJson.author,
		funding: packageJson.funding,
		keywords: packageJson.keywords,
		engines: packageJson.engines,
		publishConfig: packageJson.publishConfig,
	};

	await fs.writeFile(path.join(distDir, "package.json"), `${JSON.stringify(publishPackageJson, null, "\t")}\n`);
}

async function main() {
	await fs.mkdir(distDir, { recursive: true });
	await createDistPackageJson();

	await Promise.all([
		copyIfPresent(".npmrc"),
		copyIfPresent("eslint.config.mjs"),
		copyIfPresent("LICENSE"),
		copyIfPresent("prettier.config.js"),
		copyIfPresent("README.md"),
		copyIfPresent(path.join("src", "tsconfig.json"), "tsconfig.json"),
	]);
}

void main();
