# @elyspio/vite-eslint-config

`@elyspio/vite-eslint-config` fournit maintenant une base partagée pour les projets `Vite+` avec React, TypeScript, Oxlint et Oxfmt.

Cette version migre le projet vers `vite-plus`, mais conserve l'export `eslint.config.mjs` pour les projets qui veulent garder la préconfiguration ESLint existante en parallèle de Vite+.

## Installation

```sh
pnpm add -D vite-plus @elyspio/vite-eslint-config
```

## Utilisation

Le package expose un helper `getDefaultConfig` à utiliser depuis `vite.config.ts`.

```ts
import { defineConfig } from "vite-plus";
import { getDefaultConfig } from "@elyspio/vite-eslint-config";

const config = getDefaultConfig({ basePath: import.meta.dirname });

export default defineConfig({
	...config,
	base: "/backup",
});
```

Le helper fournit :

- les plugins React / SVGR / mkcert / Babel déjà configurés ;
- les alias dérivés du `tsconfig.json` étendu ;
- un bloc `lint` compatible `vp check` / `vp lint` ;
- un bloc `fmt` compatible `vp check` / `vp fmt`.

Si besoin, ces blocs restent modifiables avant export :

```ts
import { defineConfig } from "vite-plus";
import { getDefaultConfig } from "@elyspio/vite-eslint-config";

const config = getDefaultConfig({ basePath: import.meta.dirname });

export default defineConfig({
	...config,
	fmt: {
		...config.fmt,
		singleQuote: true,
	},
	lint: {
		...config.lint,
		ignorePatterns: [...config.lint.ignorePatterns, "coverage/**"],
	},
});
```

## TypeScript

Le preset TypeScript publié reste disponible :

```json
{
	"extends": "@elyspio/vite-eslint-config/tsconfig.json"
}
```

## ESLint

La préconfiguration ESLint historique reste exportée pour compatibilité :

```js
import config from "@elyspio/vite-eslint-config/eslint.config.mjs";

export default config;
```

## Prettier

Le fichier `prettier.config.js` est aussi restauré pour les IDE et outils qui en ont encore besoin :

```js
import config from "@elyspio/vite-eslint-config/prettier.config.js";

export default config;
```

## API OpenAPI

L'utilitaire `generateApi` est toujours exporté :

```ts
import { generateApi } from "@elyspio/vite-eslint-config";
```

## Changements importants

- `ESLint` n'est plus la surface principale du package, mais l'export `./eslint.config.mjs` reste disponible pour compatibilité.
- `Prettier` n'est plus la surface principale du package, mais l'export `./prettier.config.js` reste disponible pour compatibilité IDE.
- La configuration statique passe désormais par `vite.config.ts` et les commandes `vp check`, `vp lint`, `vp fmt` et `vp pack`.
- Le sous-chemin historique `@elyspio/vite-eslint-config/vite/vite.config` reste exporté pour faciliter la transition.
