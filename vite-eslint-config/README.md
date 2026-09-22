# @elyspio/vite-eslint-config

Base partagée pour les projets `Vite+` (≥ 0.3) avec React, TypeScript 7, Oxlint et Oxfmt.

## Installation

```sh
pnpm add -D vite-plus @elyspio/vite-eslint-config
```

Node ≥ 22.18 requis.

## Utilisation

Le package expose un helper `getDefaultConfig` à utiliser depuis `vite.config.ts`.

```ts
import { defineConfig } from "vite-plus";
import { getDefaultConfig } from "@elyspio/vite-eslint-config";

const config = getDefaultConfig({ basePath: import.meta.dirname, port: 3000 });

export default defineConfig({
	...config,
	base: "/backup",
});
```

Le helper fournit :

- les plugins React, React Compiler (Babel), SVGR et mkcert (désactivable avec `useMkcert: false`) ;
- les alias dérivés des `paths` du `tsconfig.json` publié ;
- un bloc `lint` (Oxlint : plugins React, jsx-a11y, TypeScript, linting typé) pour `vp lint` / `vp check` ;
- un bloc `fmt` (Oxfmt : tabulations, largeur 180, guillemets doubles) pour `vp fmt` / `vp check`.

Le code généré est exclu du lint et du formatage : dossiers `**/generated/**` (sortie conseillée pour `generateHeyApi` et `generateApi`) et fichiers `**/*.gen.ts`.

Ces blocs restent modifiables avant export :

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

```json
{
	"extends": "@elyspio/vite-eslint-config/tsconfig.json"
}
```

Le preset cible TypeScript 7 (`moduleResolution: "bundler"`, `jsx: "react-jsx"`, `strict`) et reste compatible TypeScript 6. Les alias (`@/*`, `@components/*`…) et `include` utilisent `${configDir}` : ils se résolvent depuis le `tsconfig.json` du projet, sans rien redéclarer. Redéclarer `paths` seulement si l'arborescence diffère.

Les imports `*.svg?react` (SVGR) ont besoin d'une déclaration de type dans le projet, `vite-plugin-svgr` n'étant pas une dépendance directe :

```ts
declare module "*.svg?react" {
	import type { FunctionComponent, SVGProps } from "react";
	const Component: FunctionComponent<SVGProps<SVGSVGElement>>;
	export default Component;
}
```

## Éditeurs

ESLint et Prettier ne sont plus fournis : Oxlint et Oxfmt lisent directement les blocs `lint` et `fmt` de `vite.config.ts`. Installer l'extension Oxc de l'éditeur (VS Code : `VoidZero.vite-plus-extension-pack` ; JetBrains : plugin Oxc). Voir `vp migrate --editor` pour générer la configuration.

## Génération de client API

### `generateHeyApi` (recommandé)

SDK typé généré par [`@hey-api/openapi-ts`](https://heyapi.dev), avec les helpers TanStack Query (`queryOptions`, `mutationOptions`, clés de requête) par défaut. `@hey-api/openapi-ts` est une dépendance optionnelle à installer dans le projet, avec **TypeScript 6** :

```sh
pnpm add -D @hey-api/openapi-ts typescript@^6
```

> hey-api utilise l'API JavaScript du compilateur TypeScript, que TypeScript 7.0 n'expose pas encore (erreur `Cannot read properties of undefined (reading 'AnyKeyword')`). Le paquet `typescript` du projet reste donc en 6.x ; le typecheck de `vp check` passe par tsgolint, qui embarque TypeScript 7.

```ts
import { generateHeyApi } from "@elyspio/vite-eslint-config";

await generateHeyApi({
	input: "http://localhost:5000/openapi/v1.json",
	output: "src/core/apis/generated",
	client: "fetch", // ou "axios"
	tanstackQuery: true,
});
```

### `generateApi` (historique)

Client `typescript-axios` généré par OpenAPI Generator via `npx` (Java requis). Conservé pour les projets existants.

```ts
import { generateApi } from "@elyspio/vite-eslint-config";

await generateApi("http://localhost:5000/swagger/v1/swagger.json", "src/core/apis/rest/generated", "Api");
```

## Migration 5.x → 6.0

- **Peer `vite-plus` ^0.3.1** (au lieu de ^0.1.15) ; plus de dépendance `vite` exacte. Lancer `vp migrate` dans le projet.
- **ESLint et Prettier supprimés** : exports `./eslint.config.mjs` et `./prettier.config.js` retirés, règles reportées dans le bloc `lint` (Oxlint). Supprimer `eslint.config.*` / `prettier.config.*` du projet.
- **Décorateurs legacy supprimés** : plus de plugins Babel `proposal-decorators` / `transform-typescript-metadata`, plus d'`experimentalDecorators` / `emitDecoratorMetadata` dans le tsconfig. Un projet qui en dépend (inversify, etc.) reste en 5.x ou ajoute ses plugins Babel.
- **tsconfig TypeScript 7** : `downlevelIteration`, `esModuleInterop`, `allowSyntheticDefaultImports` et `forceConsistentCasingInFileNames` retirés ; `lib` = `ESNext` + `DOM` ; `paths` et `include` en `${configDir}` (les alias n'ont plus besoin d'être redéclarés dans le projet).
- **Plugins** : Babel 8, `vite-plugin-svgr` 5, `vite-plugin-mkcert` 2, `@vitejs/plugin-react` 6.1.
- **`fmt`** : `trailingComma: "es5"` ajouté (reprend l'ancienne config Prettier).
- **Nouveau** : `generateHeyApi`.
