import react from "@vitejs/plugin-react-swc";
import eslint from "vite-plugin-eslint";
import svgr from "vite-plugin-svgr";
import tsconfig from "../tsconfig.json";
import { UserConfig } from "vite";
import { convertPathToAlias } from "./internal.vite";
import vitePluginImport, { PluginOptions } from "vite-plugin-babel-import";
import checker from "vite-plugin-checker";

type GetConfigParams = {
	basePath?: string;
	disableChunks?: boolean;
	preserveImports?: ("@mui/icons-material" | "@mui/material")[];
	port?: number;
	tsCheck?: boolean;
	eslintCheck?: boolean;
};

export const getDefaultConfig = ({
	basePath = __dirname,
	preserveImports = [],
	disableChunks = false,
	port = 3000,
	tsCheck = false,
	eslintCheck = false,
}: GetConfigParams): UserConfig => {
	const plugins = [
		svgr(),
		react({
			tsDecorators: true,
		}),
	];

	if (tsCheck) {
		plugins.push(
			checker({
				// e.g. use TypeScript check
				typescript: true,
			}),
		);
	}
	if (eslintCheck) {
		plugins.push(
			eslint({
				failOnWarning: false,
				failOnError: true,
				fix: true,
				cache: false,
			}),
		);
	}

	if (preserveImports.length < 2) {
		plugins.push(
			vitePluginImport(
				[
					!preserveImports.includes("@mui/icons-material")
						? {
								ignoreStyles: [],
								libraryName: "@mui/icons-material",
								libraryDirectory: "",
								libraryChangeCase: "pascalCase",
							}
						: false,
					!preserveImports.includes("@mui/material")
						? {
								ignoreStyles: [],
								libraryName: "@mui/material",
								libraryDirectory: "",
								libraryChangeCase: "pascalCase",
							}
						: false,
				].filter(Boolean) as PluginOptions,
			),
		);
	}

	return {
		build: {
			rollupOptions: {
				output: {
					manualChunks(id) {
						if (disableChunks) return undefined;
						// if (id.includes("node_modules/@mui")) return "mui.vendor";
						// if (id.includes("node_modules/react/" || "node_modules/react-dom/")) return "react.vendor";
						if (id.includes("node_modules")) return "vendor";
					},
				},
			},
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

export * from "./internal.vite";
