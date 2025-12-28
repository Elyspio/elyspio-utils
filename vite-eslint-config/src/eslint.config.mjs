// eslint.config.mjs (Flat ESLint configuration for Vite + React + TypeScript)

// Import built-in configs and plugins
import js from "@eslint/js"; // Core ESLint recommended rules
import ts from "typescript-eslint"; // TypeScript plugin + parser
import react from "eslint-plugin-react"; // React core rules
import reactHooks from "eslint-plugin-react-hooks"; // React Hooks rules
import jsxA11y from "eslint-plugin-jsx-a11y"; // Accessibility rules (JSX a11y)
import { defineConfig } from "eslint/config";

export default defineConfig(
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
	react.configs.flat["jsx-runtime"] // React recommended rules
);
