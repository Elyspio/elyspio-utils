import { defineConfig } from "vite-plus";

export default defineConfig({
	fmt: {
		ignorePatterns: ["dist/**", "node_modules/**"],
		printWidth: 180,
		singleQuote: false,
		tabWidth: 4,
		useTabs: true,
	},
	lint: {
		ignorePatterns: ["dist/**", "node_modules/**"],
		options: {
			typeAware: true,
			typeCheck: true,
		},
	},
	pack: {
		clean: false,
		dts: true,
		entry: {
			index: "src/index.ts",
			"api/generate": "src/api/generate.ts",
			"vite/vite.config": "src/vite/vite.config.ts",
		},
		format: ["esm"],
		outDir: "dist/lib",
		sourcemap: true,
	},
});
