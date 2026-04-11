import js from "@eslint/js";
import ts from "typescript-eslint";
import react from "eslint-plugin-react";
import reactHooks from "eslint-plugin-react-hooks";
import jsxA11y from "eslint-plugin-jsx-a11y";
import reactRefresh from "eslint-plugin-react-refresh";
import { defineConfig, globalIgnores } from "eslint/config";
import globals from "globals";

export default defineConfig(
	globalIgnores(["dist", "node_modules"]),
	js.configs.recommended,
	reactHooks.configs.flat["recommended-latest"],
	ts.configs.recommendedTypeChecked,
	{
		languageOptions: {
			parserOptions: {
				projectService: true,
			},
		},
	},
	jsxA11y.flatConfigs.recommended,
	react.configs.flat.recommended,
	react.configs.flat["jsx-runtime"],
	reactRefresh.configs.vite,
	{
		files: ["**/*.{js,jsx,ts,tsx}"],
		ignores: ["node_modules/", "dist/"],
		languageOptions: {
			ecmaVersion: "latest",
			sourceType: "module",
			parserOptions: {
				ecmaFeatures: { jsx: true },
			},
			globals: {
				...globals.browser,
				...globals.node,
			},
		},
		settings: {
			react: { version: "detect" },
		},
		rules: {
			"@typescript-eslint/ban-ts-comment": "off",
			"@typescript-eslint/no-dynamic-delete": "off",
			"@typescript-eslint/no-explicit-any": "off",
			"@typescript-eslint/no-extraneous-class": "off",
			"@typescript-eslint/no-misused-promises": "off",
			"@typescript-eslint/no-non-null-assertion": "off",
			"@typescript-eslint/no-unsafe-declaration-merging": "off",
			"@typescript-eslint/no-unsafe-member-access": "off",
			"@typescript-eslint/no-unsafe-return": "off",
			"@typescript-eslint/no-unused-vars": [
				"warn",
				{
					argsIgnorePattern: "^_+$",
					caughtErrorsIgnorePattern: "^_+$",
					destructuredArrayIgnorePattern: "^_+$",
					varsIgnorePattern: "^React$",
				},
			],
			"@typescript-eslint/unbound-method": "off",
			"jsx-a11y/no-autofocus": "off",
			"no-console": "off",
			"react/display-name": "off",
			"react/no-unescaped-entities": "off",
			"react/prop-types": "off",
			"react-refresh/only-export-components": "off",
		},
	},
);
