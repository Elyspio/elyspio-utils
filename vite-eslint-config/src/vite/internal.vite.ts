import * as path from "path";

/** TypeScript template resolved to the directory of the extending tsconfig, i.e. the consumer project. */
const configDirPrefix = "$" + "{configDir}/";

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
		const relativeValue = aliasValue.startsWith(configDirPrefix) ? aliasValue.slice(configDirPrefix.length) : aliasValue;

		acc[aliasKey] = path.resolve(basePath, relativeValue);
		return acc;
	}, {});
}
