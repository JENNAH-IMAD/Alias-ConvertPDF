# Interface ReleveFlow

Interface Next.js pour la gestion des clients, banques, comptes bancaires,
conversions PDF, modèles d'export et archives.

`npm ci`, puis `npm run dev` (port 3001). Configurer `NEXT_PUBLIC_API_URL`
dans `.env.local` à partir de `.env.example`.

Contrôles : `npm run lint` et `npm run build`.
Voir le [README principal](../../README.md) et [l’architecture](../../docs/architecture.md).

## Parcours des relevés

- `/conversions` : sélectionner un client et son compte, puis déposer le PDF.
- `/statements/{id}` : analyser, corriger les opérations, renseigner les soldes
  et la période, valider, générer les fichiers et archiver.
- `/templates` : créer et configurer les modèles CSV/Sage avec un compte Admin.
- `/client-space` : consulter les relevés d'un client et de ses comptes.
- `/archives` : filtrer par client, banque, compte, période et présence d'exports.

Les nouveaux écrans reprennent les composants shadcn/ui, les animations Motion
et la palette monochrome claire/sombre. Les droits sont contrôlés également
par l'API. Un utilisateur consulte ses propres imports ; Admin consulte tous
les relevés. Aucun document ou compte de démonstration n'est créé au démarrage.

Le backend doit fonctionner sur le port 5080. Sans profil vérifié, un PDF reste
en « Format inconnu » et nécessite une revue manuelle. Les PDF scannés nécessitent
la configuration OCR décrite dans le [guide](../../docs/statements.md).

## Derniers ajustements

Les thèmes utilisent désormais des surfaces blanches en mode clair et ardoise en
mode sombre, avec des paires texte/fond explicites pour tous les boutons.
Après l'import, la deuxième étape permet de choisir Sage 100, Sage X3 ou CSV.
La section Fichiers du relevé permet de consulter le PDF dans une fenêtre,
de consulter/télécharger le CSV de travail et chacun des exports finaux conservés.
Archives et Espace clients restent deux pages distinctes. L’Espace clients affiche
uniquement ses cinq derniers relevés bancaires archivés, classés du plus récent
au plus ancien. La conversion et les exports restent accessibles dans le détail
de chaque archive.

Statuts de traitement et historique en français, badges de progression, validation
et erreur. Les trois modèles de la capture sont installés par migration :
Sage 100 – Standard, Sage X3 – Import Banque et CSV personnalisé, avec colonnes françaises.
La limite est de 5 relevés conservés par client, tous comptes confondus. Chaque PDF
compte dès son import ; le sixième import est refusé jusqu’à la suppression d’un relevé.
Vue d'ensemble est le premier élément du menu. La palette neutre accueille des
accents bleus et des couleurs d'état ; le menu possède son propre défilement.
