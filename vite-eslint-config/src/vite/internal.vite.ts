import * as path from "path";

/**
 * Convert typescripts "compilerOptions.paths" to vite/webpack alias
 */
export function convertPathToAlias(paths: Record<string, string[]>, basePath: string) {
	return Object.keys(paths)
		.filter((p) => p.endsWith("*"))
		.reduce((acc, key) => {
			const p = paths[key][0];
			acc[key.slice(0, key.length - 2)] = path.resolve(basePath, p.slice(0, p.length - 1));
			return acc;
		}, {});
}
