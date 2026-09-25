# Relevés, modèles d'export et archives

## Parcours livré

Après le dépôt, **l'étape 2 choisit le format d'export** : Sage 100, Sage X3 ou CSV
personnalisé. Ce choix est modifiable jusqu'à la génération et ne change pas
l'extraction du PDF. Il reste en mémoire pendant l'utilisation de la page ;
après rechargement complet, choisir à nouveau le modèle.

La section **Consulter et télécharger** distingue trois éléments :

- PDF original : aperçu intégré au navigateur ou téléchargement.
- Fichier converti : CSV de travail UTF-8 généré depuis les opérations enregistrées,
  même avant validation. Le nom indique `a_verifier` ou `valide`. Il reflète l'état
  courant des données et ne représente pas un export final immuable.
- Exports finaux : aperçu décodé avec l'encodage du modèle enregistré au moment
  de la génération, ou téléchargement exact du fichier conservé.

L'aperçu du CSV de travail est limité à 30 lignes et celui d'un export final à
32 000 caractères ; les téléchargements restent complets. Les contrôles d'accès
sont identiques à ceux du relevé. Les aperçus ne relancent pas l'extraction PDF.

1. Ouvrir **Modèles d'export** avec un compte Admin. Créer un modèle CSV,
   Sage 100 ou Sage X3, puis adapter ses colonnes au logiciel destinataire.
2. Ouvrir **Conversions**, choisir un client et l'un de ses comptes existants.
   Déposer un PDF non vide de 20 Mo maximum.
3. Dans le détail du relevé, lancer l'analyse automatique ou choisir un profil
   de la banque. La file de traitement s'exécute dans le backend.
4. Vérifier le PDF original et le texte extrait. Corriger ou saisir les opérations,
   renseigner les dates de période, le solde initial et le solde final.
5. Enregistrer la revue puis valider les données enregistrées. Les anomalies
   de date, devise, montant, doublon et solde bloquent la validation.
6. Choisir un modèle, consulter l'aperçu de dix lignes, puis générer le fichier.
   Chaque export est conservé et peut être téléchargé plusieurs fois.
7. Retrouver le relevé dans l'espace client. Il occupe une place dans l'archive
   dès son import, quel que soit son statut de traitement.

L'espace client rassemble jusqu'à cinq relevés bancaires du client sélectionné,
avec un filtre optionnel par compte et par statut. La conversion, les exports et
l'historique de chaque relevé restent accessibles depuis son détail. L'ancienne
page `/archives` redirige vers cet espace unique. Pagination : 20 relevés par page.

Un client peut posséder au maximum **5 relevés conservés**, tous comptes et auteurs
confondus. Chaque PDF compte immédiatement après son import, même si son traitement
est en cours, échoué ou à vérifier. Le sixième import est refusé avec HTTP 409.
La suppression définitive d'un relevé libère une place. L'import verrouille la ligne
client pendant le comptage, ce qui protège la limite contre les demandes simultanées.

## Accès et conservation

- Admin voit tous les relevés et gère les modèles/profils.
- User voit uniquement les relevés qu'il a importés. Les référentiels clients,
  banques et comptes restent partagés selon les permissions existantes.
- `statements.read` nécessite `clients.read` et `accounts.read`.
  `statements.write` nécessite aussi `statements.read`.
- Les permissions et l'état du compte sont revérifiés en base à chaque requête.
- Un hash SHA-256 et une contrainte unique empêchent de réimporter le même PDF
  sur le même compte, y compris simultanément.
- Les versions du relevé et du modèle empêchent d'écraser une modification concurrente.
- La suppression d'un compte utilisé par des relevés est refusée. Son client,
  sa banque, son numéro et sa devise ne peuvent plus être remplacés ; les champs
  de paramétrage comptable restent modifiables.
- Les utilisateurs supprimés ne font pas disparaître l'historique documentaire.
  Admin conserve l'accès à leurs relevés.
- Les fichiers utilisent des clés opaques. Aucun chemin physique n'est renvoyé
  dans les DTO de relevé ; aucun dossier documentaire n'est publié en statique.

Le module ne propose pas de suppression définitive de relevés ou de purge automatique.
Les sauvegardes doivent couvrir la base ET les fichiers, sur la même période.

## Modèle et services

```text
Client -> BankAccount -> BankStatement -> BankTransaction
Bank -> BankStatementProfile
BankStatement -> ProcessingHistory
BankStatement -> StatementExport -> ExportTemplate -> ExportTemplateField
```

`BankStatement` conserve l'original, le hash, la période, les soldes, l'auteur,
le statut, le profil choisi, le texte extrait, la validation et la version.
`BankTransaction` conserve les montants normalisés, les dates et le libellé,
ainsi que la ligne source et le score OCR lorsqu'ils sont disponibles.
Une correction avec l'identifiant de ligne conserve sa provenance.
`StatementExport` conserve le fichier généré et un instantané JSON du modèle.
Modifier un modèle ne modifie donc aucun ancien fichier.

| Service | Responsabilité |
| --- | --- |
| StatementService | Accès aux relevés, import, revue, validation et archivage |
| StatementWorker | File durable en base, réservation atomique et reprise |
| PdfProcessingService | Orchestration de l'analyse et de l'extraction |
| PdfAnalyzer | Lecture PdfPig, pages, texte et classification |
| TesseractOcrService | Rendu Poppler, OCR Tesseract, score et nettoyage temporaire |
| ProfileTransactionExtractor | Reconnaissance des lignes et normalisation selon profil |
| StatementValidator | Contrôles métier et rapprochement des soldes |
| ExportTemplateService | Configuration des colonnes et formats |
| DelimitedStatementExporter | Fichiers délimités UTF-8 ou Windows-1252 |
| StatementExportService | Aperçu, génération, stockage et téléchargement autorisé |
| LocalFileStorageService | Fichiers privés, hashes et clés opaques |

Interfaces Application : `IFileStorageService`, `IPdfAnalyzer`, `IOcrService`,
`IStatementValidator`, `IStatementExporter`. Les entités restent dans Domain.
Le moteur d'export consomme les opérations normalisées ; il ne relit jamais le PDF.

## Traitement PDF et OCR

Le dépôt vérifie extension, MIME, signature et taille. L'analyse asynchrone vérifie
que le document est lisible et contient entre 1 et 100 pages. Un document corrompu
ou nécessitant un mot de passe passe en échec et ne peut pas être validé sans analyse.
La classification texte/scanné/mixte est heuristique, selon le texte présent par page.

Les statuts actifs sont `QUEUED`, `ANALYZING`, `OCR_PROCESSING`. Les résultats sont
`UNKNOWN_FORMAT`, `REVIEW_REQUIRED`, `EXTRACTION_FAILED` ou `OCR_FAILED`.
Le workflow utilisateur ajoute `VALIDATED`, `EXPORTED`, `ARCHIVED`,
`VALIDATION_FAILED` et `EXPORT_FAILED`. Ces codes techniques restent internes ;
l'interface affiche les libellés français et des badges d'état avec icônes.
Les soldes ou la période ne sont jamais inventés : ils sont renseignés en revue.

La file interroge PostgreSQL toutes les deux secondes. Une mise à jour conditionnelle
réserve un relevé pour un seul worker. Un traitement interrompu depuis plus de
20 minutes devient relançable ; une exécution dispose d'un délai de 10 minutes.
Une commande OCR dispose d'un délai de 3 minutes. Limites : 2 millions de caractères,
10 000 opérations, expression régulière limitée à une seconde par ligne.
Les routes des relevés sont limitées à 60 requêtes/minute/utilisateur.

Le bouton de relance est disponible tant qu'aucune opération n'existe. Avec des
opérations, utiliser la revue pour préserver les corrections. Une erreur OCR peut
être corrigée par configuration puis relance, ou par saisie après analyse du PDF.

### Configuration locale

Installer Poppler et Tesseract avec les données `fra` et `eng`, puis définir dans
le terminal qui lance le backend, en adaptant les chemins à l'installation :

```powershell
$env:Statements__PdfToPpm = 'C:\outils\poppler\bin\pdftoppm.exe'
$env:Statements__Tesseract = 'C:\Program Files\Tesseract-OCR\tesseract.exe'
$env:Statements__OcrLanguage = 'fra+eng'
$env:Statements__StoragePath = 'C:\donnees\releveflow\statements'
./scripts/start-backend.ps1
```

Ces chemins sont des exemples, pas des outils installés automatiquement par ce script.
`.env` n'est pas chargé par .NET. Sans chemin de stockage, le backend utilise
`private-statements` dans son ContentRoot. Docker configure les exécutables et
monte le volume `statements` sur `/app/private-statements`.

### Ajouter un profil bancaire

Créer une banque dans le catalogue ne signifie pas que son PDF est pris en charge.
Obtenir plusieurs PDF anonymisés de la même structure, dont des variantes de pages,
dates, montants et libellés ; vérifier leur texte extrait avant de définir le profil.

L'API Admin `POST /api/statement-profiles` accepte `bankId`, `name`, `code`,
`version`, `isActive`, `detectionText`, `transactionPattern`, `dateFormat`,
`decimalSeparator`, `thousandsSeparator`, `ocrRequired`.
Le couple code/version est unique. Créer une nouvelle version pour une nouvelle
structure. La gestion des profils est actuellement disponible par API/Swagger.

L'extracteur actuel utilise une expression régulière par ligne avec groupes nommés
`date`, `description` (obligatoires), `valueDate`, `reference`, `debit`, `credit`,
`balance` (optionnels). Il normalise selon les séparateurs déclarés et conserve
les valeurs illisibles à NULL pour qu'elles soient corrigées.
La détection automatique exige un seul profil actif correspondant au texte de la banque.
Les profils ambigus ou absents restent en format inconnu.

Pour des tableaux complexes, remplacer ou étendre `ProfileTransactionExtractor`
avec des règles validées sur ces PDF. Ne pas ajouter un profil bancaire sur la seule
base du nom de la banque. Aucun profil de banque réelle n'est livré ni prérempli.

## Exports

La migration de données `FrenchExportPresets` installe une fois les trois modèles
de la capture, avec colonnes françaises. Elle conserve les modèles existants,
les exports et les référentiels. Supprimer un modèle ne le recrée pas au démarrage.
Le bouton Admin permet de le recréer explicitement, sans doublon de code.
Ce sont des bases configurables, pas des certifications Sage.

| Type | Extension / encodage initial | Colonnes initiales |
| --- | --- | --- |
| SAGE100_STANDARD | TXT / Windows-1252 | Date, Account, Description, Amount, Direction, Analytic |
| SAGE_X3 | CSV / UTF-8 | Date, Journal, Account, Description, Debit, Credit, Reference |
| CUSTOM_CSV | CSV / UTF-8 | Date, Description, Debit, Credit, Balance |

Les noms de colonnes visibles sont français : Date, Compte, Libellé, Montant,
Sens, Analytique pour Sage 100 ; Date, Journal, Compte, Libellé, Débit, Crédit,
Référence pour Sage X3 ; Date, Libellé, Débit, Crédit, Solde pour CSV personnalisé.
Sage utilise initialement le point-virgule ; le CSV personnalisé utilise la virgule.

Configurer l'ordre, les noms de sortie, les champs obligatoires, les valeurs par
défaut, les en-têtes, le séparateur, les décimales et le format de date.
`Account` correspond au compte comptable du compte bancaire ; `Journal` à son journal.
`Amount` est positif et `Direction` vaut D ou C. `Analytic` utilise la valeur par défaut.
Le CSV échappe les guillemets, retours à la ligne et séparateurs ; les textes pouvant
être interprétés comme formules de tableur sont préfixés d'une apostrophe.
L'encodage refuse les caractères impossibles à représenter, sans remplacement silencieux.

Ajouter un format délimité consiste à définir un modèle avec les sources proposées.
Pour un nouveau format technique, implémenter `IStatementExporter`, enregistrer son
implémentation dans `Program.cs`, puis adapter les types acceptés, les extensions,
les contrôles et l'interface. Ajouter des tests sur un fichier accepté par le logiciel cible.

## Routes

Toutes les routes ci-dessous sont authentifiées et couvertes par les permissions.

| Méthode et route | Action |
| --- | --- |
| GET /api/statements | Liste visible ; page, clientId, accountId, bankId, status, archived, from, to, exported |
| GET /api/archives | Compatibilité API : même liste, limitée aux relevés archivés |
| POST /api/statements/{id}/reopen | Retire un relevé des archives avant modification |
| DELETE /api/statements/{id} | Supprime le relevé, ses opérations, son historique et ses fichiers |
| GET /api/clients/{clientId}/archive-usage | Nombre d'archives du client et limite (5) |
| POST /api/bank-accounts/{accountId}/statements/upload | Multipart `file` |
| GET /api/statements/{id} | Détail, opérations, contrôles, historique et exports |
| POST /api/statements/{id}/process | Corps : version, profileId optionnel |
| PUT /api/statements/{id}/review | Version, période, soldes, liste d'opérations |
| POST /api/statements/{id}/validate | Version |
| POST /api/statements/{id}/archive | Version |
| GET /api/statements/{id}/pdf | Télécharger l'original |
| GET /api/statements/{id}/converted/download | CSV de travail des opérations enregistrées |
| GET /api/statements/{id}/converted/preview | Aperçu CSV de travail (30 opérations) |
| GET /api/statement-exports/{id}/preview | Aperçu du fichier final conservé |
| POST /api/statements/{id}/export-preview | Version et templateId ; dix lignes |
| POST /api/statements/{id}/exports | Version et templateId ; génération |
| GET /api/statement-exports/{id}/download | Télécharger le fichier conservé |
| GET/POST /api/export-templates | Liste / création Admin |
| PUT/DELETE /api/export-templates/{id} | Modification / suppression Admin |
| GET /api/export-templates/fields | Sources et formats de date |
| POST /api/export-templates/presets/{type} | Création explicite Admin |
| GET/POST /api/statement-profiles | Liste / création Admin |

Les corps de modification portent la version lue. Un conflit renvoie HTTP 409 ;
actualiser le relevé avant de réessayer. Aucun export ne peut être généré avant
validation. Un modèle utilisé est conservé et peut être désactivé.

## Migrations et fichiers

Les migrations `20260924161432_StatementWorkspace` et
`20260924162315_StatementIdentifiers` sont additives. La seconde précise que les
identifiants des nouvelles entités sont créés par l'application. Les migrations
historiques ne sont ni supprimées ni réécrites.

```powershell
# Configurer ConnectionStrings__Default et Jwt__Key dans ce terminal.
dotnet ef database update --project backend/BankStatementConverter.Infrastructure --startup-project backend/BankStatementConverter.API
```

Dans cette installation, `scripts/start-backend.ps1` charge les secrets privés
existants et applique les migrations. Aucun référentiel de démonstration n'est créé.

Créations principales :

- `Domain/Statements.cs`, `Application/StatementContracts.cs`.
- `Infrastructure/Statements/*.cs` et les deux migrations avec leurs designers.
- `API/Controllers/StatementsController.cs` : trois contrôleurs documentaires.
- `Tests/StatementTests.cs`, `Tests/StatementIntegrationTests.cs`.
- Pages frontend `conversions`, `statements/[id]`, `templates`, `archives`, `client-space`.
- `components/statement-list.tsx`, `services/statements.ts`, `services/use-catalog.ts`.
- `docs/statement-implementation.md` et ce guide.

Modifications : DbContext/snapshot, permissions et validation des droits,
CatalogService (protection des comptes utilisés), Program.cs, dépendance PdfPig,
navigation, page utilisateurs, styles monochromes, configuration Docker/env,
exclusions de stockage et documentation.
Aucun fichier source historique n'a été supprimé pendant cette reconstruction.

## Validation métier et limites restantes

- Aucun relevé réel de banque n'a été fourni : seule une fixture synthétique
  vérifie l'extraction automatique. La revue manuelle fonctionne pour un PDF lisible.
- L'extracteur de base est linéaire : pas encore de règles dédiées de colonnes
  positionnelles, en-têtes/pieds de page ou libellés multilignes par banque.
- Tesseract n'a pas été exécuté localement faute d'installation disponible.
  Les PDF mixtes passent actuellement entièrement par OCR ; pas d'OCR sélectif par page.
- Le rapprochement utilise une tolérance de 0,01 et l'ordre des lignes saisi.
  Vérifier cette règle avec les devises et présentations attendues.
- Les contrôles et les exports ne remplacent pas une validation humaine.
  La compatibilité Sage doit être confirmée avec la version et le paramétrage cible.
- Les erreurs d'extraction, de validation et de configuration d'export sont
  historisées. Les erreurs techniques de stockage/base remontent à l'interface
  et aux journaux, sans garantie d'écriture de l'historique si la base est indisponible.
- Pas d'affectation client/utilisateur dédiée, de suppression documentaire ou de
  téléchargement groupé. L'aperçu PDF intégré dépend du lecteur du navigateur ;
  le téléchargement est disponible si celui-ci ne permet pas l'affichage.
- Un crash entre l'écriture disque et la transaction peut laisser un fichier orphelin ;
  aucune purge automatique ne supprime des fichiers potentiellement utiles.
- L'analyse PdfPig s'exécute dans le processus API. Avant un déploiement exposé,
  isoler les traitements lourds dans un worker aux ressources limitées.
- La recette visuelle authentifiée, Docker et des PDF scannés réels restent à vérifier.

Voir [verification.md](verification.md) pour les commandes et les résultats de tests.
