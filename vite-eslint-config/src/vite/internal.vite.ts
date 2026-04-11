import * as path from "path";

/**
 * Convert typescripts "compilerOptions.paths" to vite/webpack alias
 */
export function convertPathToAlias(paths: Record<string, string[]> = {}, basePath: string) {
	return Object.entries(paths).reduce<Record<string, string>>((acc, [key, values]) => {
		const firstValue = values[0];
		if (!firstValue) {
			return acc;
		}

		const hasWildcard = key.endsWith("/*") && firstValue.endsWith("/*");
		const aliasKey = hasWildcard ? key.slice(0, -2) : key;
		const aliasValue = hasWildcard ? firstValue.slice(0, -2) : firstValue;

		acc[aliasKey] = path.resolve(basePath, aliasValue);
		return acc;
	}, {});
}
