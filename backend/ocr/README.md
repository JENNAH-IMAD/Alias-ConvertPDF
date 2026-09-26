# OCR BMCE local

Installer Node.js 22, puis exécuter `npm ci` dans ce dossier. Configurer `Ocr__Script` avec le chemin absolu de `bmce.cjs` pour un lancement depuis les sources. En publication, le script est copié sous `ocr/`; installer ses dépendances avec `npm ci --omit=dev`. Le Dockerfile prépare ces dépendances. Aucun document n’est envoyé sur Internet.

Le modèle `tessdata/eng.traineddata` provient de https://github.com/tesseract-ocr/tessdata_fast (licence Apache-2.0), téléchargé depuis `main`. SHA-256 : `7D4322BD2A7749724879683FC3912CB542F19906C83BCC1A52132556427170B2`. Tesseract.js, Sharp et leurs licences figurent dans les dépendances verrouillées par package-lock.json.

Le profil `bmce-scan` est spécialisé pour les captures BMCE quadrillées à cinq colonnes (date, date de valeur, opération, débit, crédit). Il extrait les images incorporées du PDF, détecte les lignes et cellules, puis exécute l’OCR local. Il ne constitue pas un OCR universel pour tous les formats BMCE ou toutes les banques. Il refuse les grilles non reconnues, les dates ou montants ambigus et un rapprochement des soldes incorrect. Limites : 20 pages, 40 images, 30 millions de pixels par image, délai de 3 minutes. Les fichiers temporaires sont supprimés.

Les chevauchements sont retirés uniquement quand au moins deux opérations consécutives identiques se répètent à la frontière de deux images. Les opérations identiques au sein d’une même image restent présentes. Les soldes par opération sont calculés à partir du solde initial (le relevé ne les fournit pas individuellement). Les références et axes analytiques absents restent vides. Les libellés OCR doivent être relus : un rapprochement exact des montants ne prouve pas l’exactitude de chaque caractère du libellé.

Le test optionnel `SuppliedBmcePdfReconcilesAndExportsAllFormats` utilise les variables `BMCE_TEST_PDF` et `BMCE_OCR_SCRIPT`. Le PDF privé n’est pas ajouté au dépôt. Sans BMCE_TEST_PDF, ce test local n’exécute pas l’OCR ; les tests synthétiques restent actifs.

## Lecture automatique des PDF texte

Le profil `bmce-auto` détecte les pages contenant du texte et lit directement le tableau Date / Libellé / Débit / Crédit, sans OCR. Le profil `bmce-text` force cette lecture. L’ancien profil `bmce-scan` bénéficie aussi de la détection texte. Cette structure correspond à `releve_bancaire_1.pdf`, pas à tous les modèles de relevés textuels. Les pages mixtes texte/scan sont refusées explicitement ; les structures inconnues ne produisent aucun export partiel. Limite texte : 200 pages.

Lorsque le relevé texte ne fournit ni solde ni date de valeur, ces champs restent vides. Aucun rapprochement de solde n’est annoncé. Les dates sont celles des opérations, même si elles dépassent la période de l’en-tête. Le test local activé par `BMCE_TEXT_TEST_PDF` vérifie 48 opérations, 6 520 900,00 MAD de débits et 4 225 000,00 MAD de crédits sur l’exemple fourni, ainsi que les trois exports.