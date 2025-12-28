import react from "@vitejs/plugin-react";
import svgr from "vite-plugin-svgr";
import tsconfig from "../tsconfig.json" with { type: "json" };
import { type PluginOption, UserConfig } from "vite";
import { convertPathToAlias } from "./internal.vite.js";
import mkcert from "vite-plugin-mkcert";

type FnPlugin = () => PluginOption;

type GetConfigParams = {
	basePath?: string;
	port?: number;
};

export const getDefaultConfig = ({ basePath = __dirname, port = 3000 }: GetConfigParams): UserConfig => {
	const plugins: PluginOption[] = [
		svgr(),
		react({
			babel: {
				plugins: ["babel-plugin-react-compiler"],
			},
		}),
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
