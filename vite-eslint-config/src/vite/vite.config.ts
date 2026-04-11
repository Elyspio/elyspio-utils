import react, { reactCompilerPreset } from "@vitejs/plugin-react";
import svgr from "vite-plugin-svgr";
import type { PluginOption, UserConfig } from "vite";
import { convertPathToAlias } from "./internal.vite.js";
import mkcert from "vite-plugin-mkcert";
import tsconfig from "../tsconfig.json" with { type: "json" };
import babel from "@rolldown/plugin-babel";

export const defaultFmtConfig = {
	ignorePatterns: ["dist/**", "node_modules/**"],
	printWidth: 180,
	singleQuote: false,
	tabWidth: 4,
	useTabs: true,
};

export const defaultLintConfig = {
	ignorePatterns: ["dist/**", "node_modules/**"],
	options: {
		typeAware: true,
		typeCheck: true,
	},
};

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
	const babelConfig = {
		presets: [reactCompilerPreset()],
		plugins: [
			"babel-plugin-transform-typescript-metadata",
			["@babel/plugin-proposal-decorators", { legacy: true }],
			["@babel/plugin-proposal-class-properties", { loose: true }],
		],
	} as Parameters<typeof babel>[0];

	const plugins: PluginOption[] = [svgr(), react(), babel(babelConfig)];
	if (useMkcert) {
		plugins.push(mkcert() as unknown as PluginOption);
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
