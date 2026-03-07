// eslint.config.mjs (Flat ESLint configuration for Vite + React + TypeScript)

// Import built-in configs and plugins
import js from "@eslint/js"; // Core ESLint recommended rules
import ts from "typescript-eslint"; // TypeScript plugin + parser
import react from "eslint-plugin-react"; // React core rules
import reactHooks from "eslint-plugin-react-hooks"; // React Hooks rules
import jsxA11y from "eslint-plugin-jsx-a11y"; // Accessibility rules (JSX a11y)
import reactRefresh from "eslint-plugin-react-refresh";
import { defineConfig, globalIgnores } from "eslint/config";

export default defineConfig(
	globalIgnores(["dist", "node_modules"]),
	js.configs.recommended, // ESLint core recommended rules
	reactHooks.configs.flat["recommended-latest"], // React Hooks recommended rules (ESLint 9+ flat config)
	ts.configs.recommendedTypeChecked,
	{
		languageOptions: {
			parserOptions: {
				projectService: true,
			},
		},
	},
	jsxA11y.flatConfigs.recommended, // Accessibility (a11y) recommended rules for JSX,
	react.configs.flat.recommended, // React recommended rules
	react.configs.flat["jsx-runtime"], // React recommended rules
	reactRefresh.configs.vite,
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
			"@typescript-eslint/ban-ts-comment": "off",
			"@typescript-eslint/no-dynamic-delete": "off",
			"@typescript-eslint/no-explicit-any": "off",
			"@typescript-eslint/no-non-null-assertion": "off",
			"@typescript-eslint/no-unsafe-declaration-merging": "off",
			"@typescript-eslint/no-extraneous-class": "off",
			"@typescript-eslint/no-unused-vars": [
				"warn",
				{
					argsIgnorePattern: "^_+$",
					varsIgnorePattern: "^React$",
					caughtErrorsIgnorePattern: "^_+$",
					destructuredArrayIgnorePattern: "^_+$",
				},
			],
			"jsx-a11y/no-autofocus": "off",
			"no-console": "off",
			"react/display-name": "off",
			"react/prop-types": "off",
			"react/no-unescaped-entities": "off",
			"@typescript-eslint/no-unsafe-return": "off",
			"react-refresh/only-export-components": "off",
			"@typescript-eslint/no-unsafe-member-access": "off",
			"@typescript-eslint/no-misused-promises": "off",
			"@typescript-eslint/unbound-method": "off",
		},
	}
);
