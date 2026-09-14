import babel from "@rolldown/plugin-babel";
import react, { reactCompilerPreset } from "@vitejs/plugin-react";
import type { PluginOption, UserConfig } from "vite-plus";
import type { OxfmtConfig } from "vite-plus/fmt";
import type { OxlintConfig } from "vite-plus/lint";
import mkcert from "vite-plugin-mkcert";
import svgr from "vite-plugin-svgr";
import tsconfig from "../tsconfig.json" with { type: "json" };
import { convertPathToAlias } from "./internal.vite.js";

export const defaultFmtConfig = {
	ignorePatterns: ["dist/**", "node_modules/**", "**/generated/**", "**/*.gen.ts"],
	printWidth: 180,
	singleQuote: false,
	tabWidth: 4,
	useTabs: true,
	trailingComma: "es5",
} satisfies OxfmtConfig;

export const defaultLintConfig = {
	ignorePatterns: ["dist/**", "node_modules/**", "**/generated/**", "**/*.gen.ts"],
	plugins: ["eslint", "typescript", "unicorn", "oxc", "react", "jsx-a11y"],
	env: {
		browser: true,
		node: true,
	},
	categories: {
		correctness: "error",
	},
	options: {
		typeAware: true,
		typeCheck: true,
	},
	rules: {
		"react/rules-of-hooks": "error",
		"react/exhaustive-deps": "warn",
		"react/react-in-jsx-scope": "off",
		"react/display-name": "off",
		"react/no-unescaped-entities": "off",
		"react/only-export-components": "off",
		"jsx-a11y/no-autofocus": "off",
		"no-console": "off",
		"no-unused-vars": [
			"warn",
			{
				argsIgnorePattern: "^_+$",
				caughtErrorsIgnorePattern: "^_+$",
				destructuredArrayIgnorePattern: "^_+$",
				varsIgnorePattern: "^React$",
			},
		],
		"typescript/ban-ts-comment": "off",
		"typescript/no-dynamic-delete": "off",
		"typescript/no-explicit-any": "off",
		"typescript/no-extraneous-class": "off",
		"typescript/no-misused-promises": "off",
		"typescript/no-non-null-assertion": "off",
		"typescript/no-unsafe-declaration-merging": "off",
		"typescript/no-unsafe-member-access": "off",
		"typescript/no-unsafe-return": "off",
		"typescript/unbound-method": "off",
	},
} satisfies OxlintConfig;

export type VitePlusConfigFragment = UserConfig & {
	fmt: typeof defaultFmtConfig;
	lint: typeof defaultLintConfig;
	plugins: PluginOption[];
};

export type GetConfigParams = {
	basePath?: string;
	port?: number;
	useMkcert?: boolean;
};

export const getDefaultConfig = ({ basePath = process.cwd(), port, useMkcert = true }: GetConfigParams = {}): VitePlusConfigFragment => {
	const plugins: PluginOption[] = [svgr(), react(), babel({ presets: [reactCompilerPreset()] })];
	if (useMkcert) {
		plugins.push(mkcert());
	}

	return {
		fmt: {
			...defaultFmtConfig,
		},
		lint: {
			...defaultLintConfig,
		},
		plugins,
		resolve: {
			alias: convertPathToAlias(tsconfig.compilerOptions.paths, basePath),
		},
		server: {
			port: port,
			host: "0.0.0.0",
		},
		preview: {
			port,
			host: "0.0.0.0",
		},
	};
};

export * from "./internal.vite.js";
