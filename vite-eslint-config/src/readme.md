# @elyspio/vite-eslint-config

## Description

Le package `@elyspio/vite-eslint-config` est une solution de factorisation pratique pour mes projets.

Il facilite la gestion de ESLint, Prettier et TypeScript regroupant leurs paramètres dans un seul package, ce qui permet d'économiser du temps et d'éviter les erreurs redondantes.

Vous êtes libre de les utiliser tel quel ou de le modifier selon vos besoins.


## Installation

Vous pouvez installer le package à l'aide de npm ou de yarn :

```shell
npm install @elyspio/vite-eslint-config
```

ou

```shell
yarn add @elyspio/vite-eslint-config
```

## Utilisation

Une fois le package installé, vous pouvez ajouter les configurations à votre fichier `.eslintrc.js` et `vite.config.js` comme suit :

**.eslintrc.js :**
```javascript
module.exports = {
    extends: ["@elyspio/vite-eslint-config"],
};
```
**.prettierrc.js :**
```javascript
const config = require("@elyspio/vite-eslint-config/.eslintrc.js");

module.exports = config.rules["prettier/prettier"][1];

```

**vite.config.ts :**
```typescript
import { getDefaultConfig } from "@elyspio/vite-eslint-config/vite/vite.config";
import { defineConfig } from "vite";

const config = getDefaultConfig({ basePath: __dirname });

export default defineConfig({
  ...config,
  base: "/backup",
  build: {
    ...config.build,
    outDir: "build",
  },
});
```


**tsconfig.json :**
```json
{
    "extends": "@elyspio/vite-eslint-config/tsconfig.json"
}
```





De cette manière, vous pouvez utiliser les configurations Vite, ESLint et Prettier centralisées de `@elyspio/vite-eslint-config`.

## Avantages

- Factorisation de vos configurations Vite, ESLint, TypeScript et Prettier, ce qui permet une meilleure lisibilité et une maintenance plus facile.
- Évite les erreurs de configuration redondantes.
- Permet de gagner du temps lors de la mise en place de vos outils.


### TypeScript 

Afin de raccourcir les `import` dans les fichiers typescripts les alias suivants sont disponibles : 
````json5
{
	"@/*": [
		"./src/*"
	],
	"@apis/*": [
		"./src/core/apis/*"
	],
	"@components/*": [
		"./src/view/components/*"
	],
	"@hooks/*": [
		"./src/view/hooks/*"
	],
	"@pages/*": [
		"./src/view/pages/*"
	],
	"@modules/*": [
		"./src/store/modules/*"
	],
	"@services/*": [
		"./src/core/services/*"
	],
	"@validators/*": [
		"./src/core/validators/*"
	],
	"@store": [
		"./src/store/index.ts"
	],
	"@store/*": [
		"./src/store/*"
	]
}
````

