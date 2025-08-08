// eslint.config.mjs (Flat ESLint configuration for Vite + React + TypeScript)

// Import built-in configs and plugins
import js from "@eslint/js"; // Core ESLint recommended rules
import ts from "typescript-eslint"; // TypeScript plugin + parser
import react from "eslint-plugin-react"; // React core rules
import reactHooks from "eslint-plugin-react-hooks"; // React Hooks rules
import jsxA11y from "eslint-plugin-jsx-a11y"; // Accessibility rules (JSX a11y)
// Optionally, import Prettier config and plugin for integration (see below)
import prettierConfig from "eslint-config-prettier";
import prettierPlugin from "eslint-plugin-prettier";

// Export the flat config array
export default ts.config(
	// 1️⃣ Base JS and TS Recommended Rules:
	js.configs.recommended, // ESLint core recommended rules
	ts.configs.recommended, // @typescript-eslint recommended rules
	ts.configs.strict, // @typescript-eslint "strict" rules (more opinions)

	// 2️⃣ React and JSX Rules:
	react.configs.flat.recommended, // React recommended rules (for flat config)
	react.configs.flat["jsx-runtime"], // React 17+ JSX transform support (no React import needed)
	reactHooks.configs["recommended-latest"], // React Hooks recommended rules (ESLint 9+ flat config)
	jsxA11y.flatConfigs.recommended, // Accessibility (a11y) recommended rules for JSX

	// 3️⃣ Global Settings and Pa rser Options:
	{
		files: ["**/*.{js,jsx,ts,tsx}"], // Lint JavaScript and TypeScript files in the project
		ignores: ["node_modules/", "dist/"], // Ignore dependencies and build output
		languageOptions: {
			ecmaVersion: "latest", // Support modern ECMAScript features
			sourceType: "module", // Enable ES modules (import/export)
			parserOptions: {
				ecmaFeatures: { jsx: true }, // Enable parsing JSX syntax
				// If using rules requiring type-checking, include:
				// project: "./tsconfig.json", tsconfigRootDir: __dirname
			},
			globals: {
				// Recognize common global variables (especially in browser/Vite environment)
				...(await import("globals")).browser, // e.g. window, document, etc.
				...(await import("globals")).node, // if Node globals needed (e.g. for Vite config files)
			},
		},
		settings: {
			react: { version: "detect" }, // Auto-detect React version:
		},
		rules: {
			"@typescript-eslint/no-non-null-assertion": "off",
			"react/prop-types": "off", // Disable prop-types rule (not needed with TypeScript)
			"@typescript-eslint/no-explicit-any": "off",
			"@typescript-eslint/no-unused-vars": "warn",
			"no-console": "off",
		},
	},
	prettierConfig, // Disable ESLint rules that conflict with Prettier
	{
		plugins: { prettier: prettierPlugin }, // Include Prettier plugin
		rules: { "prettier/prettier": "error" }, // Treat Prettier issues as errors
	}
);
