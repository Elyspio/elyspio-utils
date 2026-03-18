import react, { reactCompilerPreset } from "@vitejs/plugin-react";
import svgr from "vite-plugin-svgr";
import { type PluginOption, UserConfig } from "vite";
import { convertPathToAlias } from "./internal.vite.js";
import mkcert from "vite-plugin-mkcert";
import tsconfig from "../tsconfig.json" with { type: "json" };
import babel from "@rolldown/plugin-babel";

type FnPlugin = () => PluginOption;

type GetConfigParams = {
	basePath?: string;
	port?: number;
};

export const getDefaultConfig = ({ basePath = __dirname, port }: GetConfigParams): UserConfig => {
	const plugins: PluginOption[] = [
		svgr(),
		react(),
		babel({
			presets: [reactCompilerPreset()],
			plugins: [
				"babel-plugin-transform-typescript-metadata",
				["@babel/plugin-proposal-decorators", { legacy: true }],
				["@babel/plugin-proposal-class-properties", { loose: true }],
			],
		} as any),
		(mkcert as unknown as FnPlugin)(),
	];

	return {
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
