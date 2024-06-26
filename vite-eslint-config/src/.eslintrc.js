module.exports = {
	env: {
		browser: true,
		es6: true,
	},
	extends: ["eslint:recommended", "plugin:@typescript-eslint/recommended"],
	ignorePatterns: ["**/generated/v*.ts", "**/*.scss", "**/*.png", "**/*.svg"],
	parser: "@typescript-eslint/parser",
	parserOptions: {
		ecmaFeatures: {
			modules: true,
			jsx: true,
		},
		ecmaVersion: "latest",
		sourceType: "module",
	},
	plugins: ["prettier", "import", "@typescript-eslint", "react", "react-hooks"],
	rules: {
		"@typescript-eslint/ban-ts-comment": "off",
		"@typescript-eslint/explicit-function-return-type": "off",
		"@typescript-eslint/explicit-module-boundary-types": "off",
		"@typescript-eslint/no-empty-interface": "off",
		"@typescript-eslint/no-explicit-any": "off",
		"@typescript-eslint/no-non-null-assertion": "off",
		"@typescript-eslint/no-shadow": "off",
		"@typescript-eslint/no-unused-vars": [
			"warn",
			{
				argsIgnorePattern: "^_",
				caughtErrorsIgnorePattern: "^_",
				varsIgnorePattern: "^_",
			},
		],
		"@typescript-eslint/no-use-before-define": "off",
		"import/extensions": "off",
		"import/no-extraneous-dependencies": "off",
		"import/order": "off",
		"import/prefer-default-export": "off",
		"jsx-a11y/click-events-have-key-events": "off",
		"jsx-a11y/control-has-associated-label": "off",
		"jsx-a11y/interactive-supports-focus": "off",
		"jsx-a11y/no-static-element-interactions": "off",
		"linebreak-style": "off",
		"max-classes-per-file": "off",
		"no-mixed-spaces-and-tabs": "off",
		"no-shadow": "off",
		"no-unused-vars": "off",
		"prettier/prettier": [
			"error",
			{
				bracketSpacing: true,
				endOfLine: "auto",
				printWidth: 180,
				singleQuote: false,
				tabWidth: 4,
				useTabs: true,
			},
		],
		"react-hooks/exhaustive-deps": "error",
		"react-hooks/rules-of-hooks": "error",
		"react/jsx-filename-extension": [
			"warn",
			{
				extensions: [".tsx"],
			},
		],
		"react/jsx-props-no-spreading": "off",
		"react/no-array-index-key": "off",
		"react/prop-types": "off",
	},
	settings: {
		"import/resolver": {
			typescript: {
				alwaysTryTypes: true,
			},
			node: {
				extensions: [".ts", ".tsx"],
			},
		},
		react: {
			version: "detect",
		},
	},
};
