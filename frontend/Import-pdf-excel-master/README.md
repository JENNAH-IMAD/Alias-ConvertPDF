# Frontend Bank Statement Converter

Frontend existant Next.js 16 / React 19 / TypeScript / Tailwind 4, relié à l’API ASP.NET Core.

Depuis ce dossier : copier `.env.example` vers `.env.local`, puis `npm ci` et `npm run dev`.

API par défaut : http://localhost:5081/api. Variable publique : `NEXT_PUBLIC_API_URL`. Aucun secret backend ne doit être ajouté ici.

Vérifications : `npm run lint` et `npm run build`. La compilation télécharge les polices Google Inter et Manrope déjà utilisées par l’interface.

Voir [le README principal](../../README.md) pour PostgreSQL, les migrations, les rôles, Docker et la conversion de démonstration.
