from pathlib import Path
from xml.sax.saxutils import escape
from reportlab.pdfgen import canvas
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak, Flowable
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib import colors
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib.enums import TA_LEFT

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'output/pdf/Rapport_avancement_ReleveFlow.pdf'
OUT.parent.mkdir(parents=True, exist_ok=True)
FONT = Path('C:/Windows/Fonts')
pdfmetrics.registerFont(TTFont('Report', str(FONT/'arial.ttf')))
pdfmetrics.registerFont(TTFont('ReportBold', str(FONT/'arialbd.ttf')))
pdfmetrics.registerFontFamily('Report', normal='Report', bold='ReportBold', italic='Report', boldItalic='ReportBold')
NAVY=colors.HexColor('#10243A'); BLUE=colors.HexColor('#1769AA'); PALE=colors.HexColor('#EDF4FA'); GRAY=colors.HexColor('#526476')
styles=getSampleStyleSheet()
styles.add(ParagraphStyle(name='BodyR', fontName='Report', fontSize=9.5, leading=14, textColor=NAVY, spaceAfter=9))
styles.add(ParagraphStyle(name='TitleR', fontName='ReportBold', fontSize=23, leading=28, textColor=NAVY, spaceAfter=19))
styles.add(ParagraphStyle(name='SubR', fontName='ReportBold', fontSize=12, leading=16, textColor=BLUE, spaceBefore=11, spaceAfter=7))
styles.add(ParagraphStyle(name='SmallR', fontName='Report', fontSize=8, leading=11, textColor=GRAY, spaceAfter=6))
styles.add(ParagraphStyle(name='CellR', fontName='Report', fontSize=8.2, leading=11.5, textColor=NAVY))
styles.add(ParagraphStyle(name='HeadR', fontName='ReportBold', fontSize=8.2, leading=11.5, textColor=colors.white))
story=[]
def p(t, sty='BodyR'): return Paragraph(t,styles[sty])
def text(t): story.append(p(t))
def sub(t): story.append(p(t,'SubR'))
def bullets(items):
    for t in items: story.append(p('• '+t))
def table(headers,rows,widths):
    data=[[p(escape(t),'HeadR') for t in headers]]+[[p(str(t),'CellR') for t in row] for row in rows]
    t=Table(data,colWidths=widths,repeatRows=1,hAlign='LEFT')
    t.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),BLUE),('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),9),('RIGHTPADDING',(0,0),(-1,-1),9),('TOPPADDING',(0,0),(-1,-1),8),('BOTTOMPADDING',(0,0),(-1,-1),8),('ROWBACKGROUNDS',(0,1),(-1,-1),[colors.white,PALE]),('LINEBELOW',(0,0),(-1,0),1,BLUE)]))
    story.append(t);story.append(Spacer(1,8))
def page(n,title,source):
    if story: story.append(PageBreak())
    story.append(p('RAPPORT D’AVANCEMENT  /  '+n,'SmallR'))
    story.append(p(title,'TitleR'))
    if source: story.append(p('Base de l’analyse : '+source,'SmallR'))
def note(t):
    t=Table([[p(t)]],colWidths=[499]);t.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,-1),PALE),('BOX',(0,0),(-1,-1),.5,colors.HexColor('#CBDFEE')),('LEFTPADDING',(0,0),(-1,-1),12),('RIGHTPADDING',(0,0),(-1,-1),12),('TOPPADDING',(0,0),(-1,-1),10)]));story.append(t)

class Architecture(Flowable):
    def __init__(self):
        super().__init__()
        self.width=499
        self.height=272
    def draw(self):
        c=self.canv
        boxes=[(0,210,499,48,'INTERFACE WEB','Next.js / React / TypeScript'),(0,137,499,48,'API ASP.NET CORE','JWT, autorisations, contrôleurs, validation'),(0,64,240,48,'SERVICES MÉTIER','Référentiels, conversions, archives'),(259,64,240,48,'MOTEURS DE TRAITEMENT','PdfPig, parsers BMCE, OCR, export'),(0,0,240,40,'POSTGRESQL','Métadonnées, profils, historique'),(259,0,240,40,'STOCKAGE PRIVÉ','PDF originaux, CSV et TXT')]
        for x,y,w,h,a,b in boxes:
            c.setFillColor(PALE);c.setStrokeColor(colors.HexColor('#B7D4E9'));c.roundRect(x,y,w,h,7,fill=1,stroke=1)
            c.setFillColor(BLUE);c.setFont('ReportBold',9);c.drawCentredString(x+w/2,y+h-16,a)
            c.setFillColor(NAVY);c.setFont('Report',8);c.drawCentredString(x+w/2,y+11,b)
        c.setStrokeColor(BLUE)
        for x,y1,y2 in [(249,210,185),(120,137,112),(379,137,112),(120,64,40),(379,64,40)]:
            c.line(x,y1,x,y2);c.line(x,y2,x-3,y2+5);c.line(x,y2,x+3,y2+5)

page('ÉDITION DU 24 SEPTEMBRE 2026','ReleveFlow','')
story.append(Spacer(1,27))
story.append(Paragraph('Rapport détaillé<br/>d’avancement de l’application',ParagraphStyle('Cover',parent=styles['TitleR'],fontSize=31,leading=39)))
story.append(Spacer(1,20))
text('Conversion des relevés bancaires PDF en fichiers comptables CSV et TXT')
story.append(Spacer(1,23))
table(['Périmètre','Contenu du rapport'],[
('Fonctionnel','Gestion des clients, banques, comptes, profils, exports, documents et archives.'),
('Technique','Architecture, technologies, modèle de données, API, sécurité et exploitation.'),
('Avancement','Implémentations constatées, vérifications, limites et travaux proposés.'),
('Scénarios','Parcours nominaux, erreurs, reprise de traitement, concurrence et suppressions.')],[110,389])
story.append(Spacer(1,23))
note('<b>État observé dans le dépôt local.</b> Ce document décrit le code disponible au 24 septembre 2026. Il ne constitue pas une certification de production ni une validation des imports dans Sage. Les résultats historiques et les contrôles exécutés pour ce rapport sont distingués.')
story.append(Spacer(1,25))
text('<b>Nom technique :</b> Bank Statement Converter<br/><b>Interface :</b> ReleveFlow<br/><b>Destinataires :</b> encadrement du projet, équipe technique et utilisateurs métier')

page('01','Synthèse et lecture du rapport','S1 à S13 ; lecture du code et test backend local.')
text('ReleveFlow dispose d’un socle applicatif intégré : interface web, API authentifiée, base PostgreSQL et stockage de fichiers. Le parcours principal permet d’associer un PDF à un client et à un compte, de sélectionner une méthode de lecture puis de produire un export comptable. Les traitements peuvent être consultés dans l’historique et dans un espace clients partagé.')
note('<b>Appréciation de l’avancement :</b> périmètre fonctionnel principal implémenté, avec plusieurs parcours techniques testés. La recette métier sur les formats réels, les interactions visuelles et la préparation à la production restent à finaliser. Aucun pourcentage global n’est attribué : aucun backlog de référence pondéré n’est disponible.')
sub('Repères de lecture')
table(['Pages','Thèmes'],[
('3 à 4','Objectifs, périmètre, acteurs et permissions'),('5 à 10','Détail des gestions réalisées'),('11 à 13','Scénarios et règles de traitement'),('14 à 17','Architecture, technologies, données et API'),('18 à 20','Sécurité, exploitation, qualité et avancement'),('21 à 22','Suite du projet et sources de traçabilité')],[75,424])
sub('Niveaux de preuve employés')
bullets(['<b>Implémenté :</b> comportement retrouvé dans les sources. Cela ne signifie pas que toutes ses variantes ont été testées.', '<b>Vérifié dans cette session :</b> 26 tests backend réussis, hors intégration PostgreSQL et PDF privés optionnels.', '<b>Vérification antérieure :</b> résultats rapportés par la documentation du projet, sans nouvelle exécution complète.', '<b>À valider / proposé :</b> action de recette ou évolution ; elle ne doit pas être présentée comme déjà livrée.'])

page('02','Présentation et périmètre métier','S1, S2, S4, S5, S6.')
sub('Problème traité')
text('Les relevés bancaires sont reçus sous forme de PDF, alors que les logiciels comptables attendent des données structurées. L’application réduit la ressaisie en extrayant les opérations, en normalisant dates et montants, puis en appliquant un modèle d’export réutilisable.')
sub('Chaîne de valeur')
table(['Étape','Résultat attendu'],[('1. Référencer','Identifier le client, sa banque et son compte bancaire.'),('2. Importer','Conserver le PDF original dans un stockage privé.'),('3. Interpréter','Lire les lignes selon un profil compatible avec la structure du PDF.'),('4. Normaliser','Produire des opérations avec date, libellé, débit, crédit et champs disponibles.'),('5. Exporter','Générer le CSV standard et le fichier comptable configuré.'),('6. Consulter','Contrôler les opérations et retrouver les fichiers conservés.')],[105,394])
sub('Utilisateurs visés et limites de périmètre')
text('Le modèle actuel correspond à un cabinet unique : les utilisateurs authentifiés partagent les référentiels et l’espace clients. Il n’existe pas de séparation par cabinet ou organisation dans les entités examinées. Le rôle Administrateur protège les paramètres bancaires et les modèles.')
bullets(['Entrée : PDF texte délimité ou structures BMCE explicitement prises en charge.', 'Sorties : Sage 100 en TXT, Sage X3 en CSV et CSV configurable. Les CSV peuvent être ouverts dans un tableur ; aucun générateur XLSX natif n’a été identifié.', 'La création d’une banque dans le référentiel ne crée pas automatiquement un parser compatible avec ses relevés.', 'Aucune connexion bancaire directe, écriture dans Sage ou validation comptable indépendante n’est établie par le code examiné.'])

page('03','Acteurs, sessions et permissions','S2 : contrôleurs et authentification ; S9 : contexte de session.')
table(['Action','Visiteur','Utilisateur','Admin'],[
('Inscription et connexion','Oui','Oui','Oui'),('Clients et comptes : consulter / gérer','Non','Oui','Oui'),('Banques : consulter','Non','Oui','Oui'),('Banques et logos : modifier','Non','Non','Oui'),('Profils et modèles : consulter','Non','Oui','Oui'),('Profils et modèles : modifier','Non','Non','Oui'),('Importer et traiter des PDF','Non','Oui','Oui'),('Historique personnel et export associé','Non','Ses traitements','Tous'),('Espace clients et documents partagés','Non','Oui','Oui'),('Suppression complète d’une banque','Non','Non','Oui'),('Suppression complète d’un client','Non','Oui','Oui')],[253,68,89,89])
sub('Gestion de la session')
text('L’inscription crée toujours un utilisateur de rôle User. Le mot de passe doit contenir 12 à 128 caractères, au moins une majuscule, une minuscule et un chiffre. Les mots de passe sont hachés avec ASP.NET PasswordHasher. Une connexion valide retourne un JWT valable 60 minutes et les informations de l’utilisateur.')
text('Le navigateur conserve le jeton dans sessionStorage et gère l’expiration ou une réponse 401. Le rôle est contenu dans le jeton : un changement de rôle nécessite une nouvelle connexion pour être reflété. Aucun écran complet de gestion des utilisateurs, de récupération du mot de passe ou de double authentification n’a été identifié.')
note('<b>Distinction essentielle :</b> l’historique est filtré par propriétaire, sauf pour Admin. Les API de l’espace clients, de traitement, de modification et de suppression des documents sont partagées entre opérateurs authentifiés ; elles ne se limitent pas au propriétaire initial.')

page('04','Gestion des clients','S2, S3, S7, S9 : clients, photos et suppression.')
sub('Informations gérées')
text('Chaque client possède un nom, une raison sociale, un ICE, un identifiant fiscal IF, un registre de commerce RC, une adresse e-mail, un téléphone et un pays. Les banques liées sont déduites des comptes du client. Les listes sont paginées ; les états de chargement, d’absence de données et d’erreur sont affichés.')
table(['Fonction','Comportement constaté'],[
('Créer / modifier','Formulaire relié aux endpoints clients. Validation des champs avant persistance.'),('Identifier','ICE unique de 15 chiffres. Nom et raison sociale obligatoires ; validation du téléphone et de l’e-mail.'),('Illustrer','Avatar à initiales ; import, remplacement et suppression de photo ou logo PNG/JPEG, 2 Mo maximum.'),('Afficher une photo','Image stockée en base ; version exposée dans la liste et chargement séparé via une route authentifiée.'),('Contacter','Liens cliquables pour e-mail et téléphone dans l’interface.'),('Supprimer','Parcours détaillé indiquant comptes, documents et fichiers impactés ; suppression transactionnelle après confirmation.')],[111,388])
sub('Scénario de gestion')
text('L’opérateur crée le client avec ses données légales, ajoute éventuellement une photo, puis crée un ou plusieurs comptes bancaires. Il retrouve ensuite les documents du client dans Espace clients. Une modification des coordonnées agit sur le référentiel partagé.')
sub('Cas de refus et précisions')
bullets(['ICE déjà utilisé : conflit en base ; ICE mal formé ou e-mail invalide : validation refusée.', 'Suppression simple avec dépendances : refus conservateur. Le parcours de suppression complète affiche les conséquences.', 'Supprimer un client conserve les banques, les profils bancaires et les modèles d’export.', 'Le statut « Actif » du DTO client est actuellement une valeur fixe ; aucun cycle d’activation/désactivation de client n’est établi.'])

page('05','Gestion des banques et des comptes','S2, S3 : BankService, CatalogService et validations.')
sub('Banques')
text('Les administrateurs créent et modifient le nom, le code et la description. Le code est nettoyé et converti en majuscules. Il accepte 1 à 30 lettres, chiffres, tirets ou underscores, avec un premier caractère alphanumérique ; il doit être unique. Le nom est limité à 200 caractères et la description à 2 000.')
bullets(['Recherche serveur sur nom, code et description ; tri par nom croissant, décroissant ou code.', 'Filtre « avec profil actif » / « à configurer », calculé à partir des profils réellement liés.', 'Compteurs des clients distincts, comptes, profils et profils actifs ; listes paginées des comptes et profils dans les détails.', 'Logo PNG/JPEG, 2 Mo maximum : ajout, remplacement et suppression réservés à Admin ; lecture authentifiée.'])
sub('Comptes bancaires')
table(['Champ','Rôle'],[('Client et banque','Associations obligatoires vers des référentiels existants.'),('Numéro et intitulé','Identification du compte ; unicité du numéro au sein d’une banque.'),('Devise','Trois lettres majuscules, avec MAD comme valeur par défaut du modèle.'),('Journal','Code transmis aux exports ; valeur par défaut BQ.'),('Compte comptable','Code comptable exporté ; valeur par défaut 512000. Distinct du numéro bancaire.')],[132,367])
text('Les utilisateurs authentifiés gèrent les comptes. Un compte permet de rattacher les opérations au bon client et de compléter automatiquement leur compte comptable et leur journal. L’existence des associations est contrôlée côté serveur.')
note('La suppression simple d’une banque avec comptes ou profils est bloquée. La suppression complète passe par un récapitulatif et conserve les clients. Une banque affichée « Profil actif » n’est pas, à elle seule, une preuve de compatibilité avec tous ses PDF.')

page('06','Profils d’extraction et lecture PDF','S3, S5, S6 : validation, parsers et documentation OCR.')
table(['Profil','Usage et limites'],[
('delimited','Lit des lignes séparées par point-virgule, tabulation ou barre verticale. Ne reconstitue pas tous les tableaux visuels.'),('bmce-text','Lecture directe de la structure textuelle Date / Libellé / Débit / Crédit reconnue.'),('bmce-auto','Détecte le texte pris en charge ; utilise l’OCR local pour les scans compatibles.'),('bmce-scan','Spécialisation BMCE sur grille à cinq colonnes ; bénéficie aussi de la détection texte.')],[102,397])
sub('Paramétrage réalisé')
text('Un profil appartient à une banque et possède un nom, une description, une clé de parser et un état actif/inactif. Pour les lignes délimitées, les paramètres incluent le séparateur, la culture fr-FR ou en-US, le format de date, les lignes initiales à ignorer et les indices de colonnes. Les indices sont distincts, entre 0 et 50 ; les lignes ignorées sont limitées à 100. Les profils sont consultables par tous les utilisateurs authentifiés et modifiables par Admin.')
sub('OCR BMCE local')
text('Le moteur extrait les images incorporées au PDF, repère les cellules de grille, puis utilise Tesseract.js avec Sharp. Le traitement décrit reste local. Les limites annoncées sont 20 pages, 40 images, 30 millions de pixels par image et un délai de 3 minutes. Les fichiers temporaires sont supprimés.')
bullets(['Une structure inconnue, des dates/montants ambigus ou un rapprochement incorrect du scan provoquent un refus.', 'Les répétitions à la frontière de deux images sont retirées seulement si au moins deux opérations consécutives identiques se répètent ; les doublons internes ne sont pas supprimés arbitrairement.', 'Pour le texte : maximum 200 pages ; les documents mêlant pages texte et pages scannées sont refusés par le parcours BMCE automatique.', 'Les libellés OCR doivent être relus. La justesse des totaux ne prouve pas celle de chaque caractère.'])

page('07','Modèles d’export et données produites','S3, S4, S5 : validation, ConversionService et exporters.')
table(['Format livré','Colonnes initiales du modèle'],[
('Sage 100 / TXT','Date, Journal, Compte, Libellé, Débit, Crédit, Référence.'),('Sage X3 / CSV','Date, Compte, Libellé, Montant, Sens, Analytique.'),('CSV configurable','Date, Libellé, Débit, Crédit, Solde.')],[128,371])
sub('Gestion des modèles')
text('Admin peut créer, modifier, désactiver et supprimer les modèles lorsque les contraintes de référence le permettent. Le paramétrage comprend le type, l’extension compatible, le séparateur, l’encodage UTF-8 ou Windows-1252, la culture numérique, la présence d’en-tête et une liste de 1 à 30 champs. Chaque champ possède un nom, une source, une position unique, un format et un indicateur obligatoire.')
sub('CSV standard conservé avec chaque réussite')
text('Le code actuel génère 12 colonnes : Date, ValueDate, Reference, Description, Debit, Credit, Balance, Amount, Direction, Account, Journal et Analytic. Il utilise UTF-8, un point-virgule comme séparateur, des dates yyyy-MM-dd et le point décimal. La présence de ValueDate est une évolution par rapport à une ancienne description du README.')
sub('Règles financières et de formatage')
bullets(['Montant signé : <b>Amount = Credit - Debit</b>. Sens : D lorsque le montant est négatif, sinon C. Si le champ Amount est ajouté à un export Sage 100, cet exporter utilise sa valeur absolue.', 'Account et Journal viennent du compte bancaire configuré ; ils ne sont pas inférés du texte du relevé.', 'Références, analytique, dates de valeur et soldes absents ne sont pas inventés. Un champ rendu obligatoire peut empêcher l’export s’il manque.', 'CsvHelper traite les séparateurs et les guillemets. Les valeurs monétaires sont normalisées en decimal côté backend.', 'Le choix d’un modèle Sage ne certifie pas sa compatibilité avec l’installation cible. Les fichiers doivent être essayés avec le paramétrage d’import réel.'])
note('L’application produit un fichier ; elle n’envoie pas directement les écritures à Sage et ne génère pas automatiquement des contreparties comptables.')

page('08','Documents, conversion et contrôle','S2, S4, S9 : documents, conversion et composants de revue.')
sub('Parcours guidé en trois étapes')
table(['Étape','Fonctionnalités réalisées'],[
('Document importé','Sélection client/compte, téléversement du PDF et conservation d’un document en attente.'),('Lecture et mapping','Sélection d’un profil et d’un export actifs ; affichage des correspondances réelles, formats, champs obligatoires et encodage.'),('Résultat et export','Affichage des transactions, totaux débit/crédit, soldes disponibles et téléchargement du fichier.')],[126,373])
text('L’import et le traitement sont séparés dans l’API. Un PDF peut donc rester en attente avant de choisir ses règles de lecture. L’ancien endpoint de conversion directe réalise les deux opérations successivement. Le traitement génère ses fichiers avant d’afficher le résultat : les étapes visibles ne constituent pas une file de tâches distribuée.')
sub('Gestion du document importé')
bullets(['États Pending et Failed : modification du client, du compte et remplacement facultatif du PDF ; retour à Pending et remise à zéro du choix des modèles.', 'Relance : un document Pending ou Failed peut être traité avec un profil compatible et un modèle actif.', 'Un document Processing ne peut pas être supprimé ; un document Completed ne peut pas être retraité par le même parcours.', 'La suppression retire l’historique et programme le nettoyage des fichiers via la file persistante.'])
sub('Consultation des opérations')
text('Le résultat fournit une recherche par libellé, référence ou date, un filtre débits/crédits et une pagination de 20 opérations. Les totaux décrivent l’ensemble des transactions fournies à la revue, pas uniquement la page visible. Lorsque l’aperçu est tronqué, les totaux portent seulement sur les données de cet aperçu. Un solde initial reconstitué est signalé comme tel et ne vaut pas rapprochement indépendant.')

page('09','Historique, espace clients et interface','S4, S7, S9 : historique, archives et pages frontend.')
sub('Historique et tableau de bord')
text('L’historique expose le client, la banque, les noms des fichiers, l’état, les dates, le nombre d’opérations et le message d’erreur éventuel. Les filtres serveur portent sur la recherche et le statut. Le tableau de bord compte les clients, banques, conversions, réussites et échecs, et présente les cinq traitements les plus récents accessibles à l’utilisateur.')
sub('Espace clients et conservation')
text('L’espace partagé regroupe les documents par client. Pour une réussite récente, trois fichiers sont disponibles : original PDF, CSV standard et export comptable choisi. L’aperçu CSV est limité à 500 lignes lues ; l’aperçu TXT à 100 000 caractères. Le téléchargement donne accès au fichier conservé, indépendamment de la limite de prévisualisation.')
note('<b>Règle de rétention :</b> seuls les cinq derniers traitements réussis sont conservés par client, tous comptes et utilisateurs confondus. L’ordre est celui des dates de réussite, pas celui des périodes des relevés. Une sixième réussite évince la plus ancienne ; un échec n’évince aucune réussite.')
text('Les éléments évincés sont supprimés de la base. Leurs fichiers sont ajoutés à une file durable, nettoyée toutes les cinq secondes par lots pouvant atteindre 100 entrées. Les suppressions en erreur sont réessayées après redémarrage. Les anciens traitements peuvent ne pas disposer de CSV standard ; l’interface signale cette absence.')
sub('Interface et paramètres')
bullets(['Navigation : tableau de bord, clients, banques, comptes, profils, modèles, conversions, historique, espace clients et paramètres.', 'Thèmes clair/sombre mémorisés localement ; navigation mobile sous forme de menu latéral.', 'Boîtes de dialogue avec gestion native du focus et fermeture par Échap ; animations respectant la préférence de réduction du mouvement.', 'Paramètres : informations de session, rôle, choix du thème et activité récente. Ce n’est pas un écran de modification du mot de passe ou de configuration serveur.'])

page('10','Scénarios nominaux de bout en bout','Scénarios déduits de S2 à S9 ; recette exhaustive non exécutée.')
table(['Scénario','Préconditions et déroulement','Résultat attendu'],[
('S01 - Préparer le cabinet','Admin se connecte, crée la banque, configure un profil et vérifie un modèle d’export. L’opérateur ajoute un client et son compte.','Référentiels cohérents prêts pour un import.'),
('S02 - PDF texte délimité','Importer un PDF compatible ; choisir le profil delimited avec les bons indices, culture et date ; lancer le traitement.','Opérations normalisées, CSV standard et export choisi.'),
('S03 - BMCE texte','Choisir bmce-auto ou bmce-text sur la structure textuelle reconnue.','Lecture sans OCR ; soldes et dates de valeur absents laissés vides.'),
('S04 - BMCE scanné','Utiliser un scan quadrillé compatible et un environnement OCR configuré.','Lecture locale, contrôle des montants et reconstitution des soldes selon le parser.'),
('S05 - Préparer puis traiter','Importer le document maintenant, revenir ensuite sur le document en attente et choisir les modèles.','Passage Pending → Processing → Completed.'),
('S06 - Retrouver les pièces','Ouvrir Espace clients, sélectionner le client puis un traitement conservé.','Consultation et téléchargement des fichiers disponibles.'),
('S07 - Changer de sortie','Pour comparer plusieurs formats, effectuer un nouvel import du PDF avec un autre modèle.','Nouveau traitement indépendant ; la rétention par client reste applicable.')],[108,248,143])
sub('Contrôle métier conseillé avant utilisation')
text('Comparer le nombre d’opérations, les dates, les montants, les libellés et les totaux avec le PDF source. Vérifier le compte comptable et le journal. Tester ensuite l’import du fichier dans un environnement Sage de recette, selon les spécifications du cabinet.')

page('11','Scénarios d’erreur et reprise','S2 à S6 ; validations et gestion des exceptions.')
table(['Situation','Réponse prévue / effet','Action de l’utilisateur'],[
('S08 - PDF faux ou trop volumineux','Refus sur extension, MIME, signature ou taille effective ; aucun document créé si rejet avant enregistrement.','Choisir un PDF valide respectant la limite.'),
('S09 - Compte incompatible','Le compte ne correspond pas au client : validation refusée.','Corriger l’association sélectionnée.'),
('S10 - Profil inactif ou autre banque','Refus avant la prise en charge du traitement.','Choisir un profil actif de la banque du compte.'),
('S11 - Ligne ou montant invalide','Échec du traitement ; aucun export partiel ; original conservé.','Corriger le profil ou remplacer le document puis relancer.'),
('S12 - Scan non reconnu / PDF mixte','Structure non prise en charge, ambiguïté ou mélange texte/scan : refus explicite.','Utiliser un format pris en charge ou développer un parser adapté.'),
('S13 - Champ d’export absent','Un champ obligatoire est vide : l’export est refusé.','Adapter le modèle si le champ n’est pas requis métier.'),
('S14 - Session expirée / rôle insuffisant','401 pour session invalide ; 403 pour action Admin interdite.','Se reconnecter ou solliciter un administrateur.'),
('S15 - Conflit de référentiel','Code de banque, ICE ou autre contrainte unique en conflit : 409.','Réutiliser l’existant ou corriger la valeur.'),
('S16 - Trop de requêtes','429 en cas de limite atteinte sur authentification ou conversion.','Attendre puis relancer sans multiplier les tentatives.')],[127,222,150])
text('Les erreurs API comportent un message, un statut et un identifiant de trace. Une erreur inattendue renvoie un message générique ; le détail technique est destiné aux journaux. Les sources distinguent les refus avant traitement des erreurs après passage à Processing.')

page('12','Concurrence, suppression et rétention','S4, S7 : archives, gestion des documents et CatalogDeletionService.')
table(['Scénario','Règle de protection','Conséquence'],[
('S17 - Double clic de traitement','Prise en charge conditionnelle sur l’état et UpdatedAt.','Une seule prise en charge ; autre tentative refusée avec conflit.'),
('S18 - Sixième réussite','Verrou transactionnel PostgreSQL par client lors de la finalisation.','Les cinq réussites les plus récentes sont conservées, même avec finalisations simultanées.'),
('S19 - Échec avec cinq archives','La rétention est appliquée seulement après réussite.','Les cinq archives réussies restent disponibles.'),
('S20 - Suppression de banque','Récapitulatif des comptes, profils, documents et fichiers ; confirmation explicite.','Suppression des dépendances concernées ; clients conservés.'),
('S21 - Suppression de client','Récapitulatif puis suppression transactionnelle.','Comptes et documents supprimés ; banques et modèles conservés.'),
('S22 - Récapitulatif devenu ancien','Comparaison d’une version calculée sur les éléments et leurs dates de mise à jour.','409 ; rouvrir la confirmation pour afficher les impacts à jour.'),
('S23 - Traitement actif','Suppression complète interdite si un document est Processing.','Attendre la fin puis actualiser le récapitulatif.'),
('S24 - Fichier temporairement verrouillé','Entrée conservée dans PendingFileDeletions en cas d’erreur de nettoyage.','Nouvelle tentative automatique ; reprise après redémarrage.')],[123,224,152])
text('La suppression complète utilise une transaction et un verrou court sur les tables concernées. Les fichiers ne sont planifiés pour nettoyage qu’avec les changements validés en base. Ce mécanisme ne doit pas être assimilé à un archivage réglementaire de longue durée ou à une corbeille restaurable.')

page('13','Architecture de l’application','S1, S2, S4 à S8 ; schéma de synthèse du code.')
story.append(Architecture());story.append(Spacer(1,15))
sub('Organisation en couches')
table(['Projet','Responsabilité'],[
('Domain','Entités métier, historique et représentation des opérations bancaires.'),('Application','DTO, contrats de services et exceptions applicatives.'),('Infrastructure','EF Core, authentification, référentiels, parsers, exports et stockage.'),('API','Routes HTTP, configuration, authentification JWT, autorisations et erreurs.'),('Tests','Tests xUnit du pipeline et intégration HTTP/PostgreSQL.')],[115,384])
text('Le frontend communique avec l’API par HTTP/JSON et multipart pour les fichiers. L’injection de dépendances assemble les services, parsers et exporters. Les interfaces IBankStatementParser et ITransactionExporter permettent d’ajouter des stratégies. Plusieurs contrôleurs documentaires accèdent aussi directement au DbContext : la séparation en couches n’est donc pas uniforme sur tous les parcours.')
text('Le traitement de conversion s’effectue dans le contexte de la requête API. Le service de fond identifié est le nettoyage des fichiers ; aucune file de conversion durable ni orchestrateur distribué n’a été identifié.')

page('14','Technologies et dépendances','S8 : versions déclarées dans package.json, csproj et Dockerfiles.')
table(['Composant','Technologie déclarée','Utilité'],[
('Frontend','Next.js 16.3.5 ; React / React DOM 19.2.8','Pages web et composants interactifs.'),
('Langage / style','TypeScript ^5 ; Tailwind CSS ^4','Typage du client API et présentation.'),
('Interface / qualité','lucide-react ^1.47.0 ; ESLint ^9','Icônes et contrôle statique frontend.'),
('Backend','C# / .NET 8 ; ASP.NET Core','API REST, DI, middleware et services.'),
('Persistance','EF Core 8.0.22 ; Npgsql EF 8.0.11','Accès et migrations PostgreSQL.'),
('Base de données','PostgreSQL 16-alpine dans Compose','Stockage relationnel ; PostgreSQL 18 cité pour les essais locaux.'),
('Lecture PDF','PdfPig 0.1.11','Extraction de texte et accès au contenu PDF.'),
('CSV','CsvHelper 33.1.0','Construction et lecture des fichiers délimités.'),
('OCR local','Tesseract.js ^7.0.0 ; Sharp ^0.35.4','Reconnaissance des caractères et traitement des images.'),
('Sécurité','JWT Bearer 8.0.22 ; Tokens.Jwt 8.14.0','Authentification et validation des jetons.'),
('Documentation API','Swashbuckle.AspNetCore 6.9.0','Génération Swagger / OpenAPI.'),
('Livraison et tests','Docker Compose ; xUnit ; WebApplicationFactory','Services conteneurisés et tests backend.')],[100,191,208])
text('Les versions ci-dessus décrivent le dépôt ; elles ne sont pas une recommandation sur les versions les plus récentes. Les plages avec ^ sont résolues par les fichiers package-lock.json. Le README principal demande Node.js 24 ; le guide OCR mentionne Node.js 22. Ces prérequis doivent être harmonisés dans la documentation d’exploitation.')

page('15','Modèle de données et intégrité','S10 : Entities.cs, AppDbContext.cs et migrations.')
table(['Entité','Contenu / liens principaux'],[
('User','Nom utilisateur, e-mail, empreinte du mot de passe, rôle ; auteur des conversions.'),
('Client','Identité légale, contacts et photo ; possède plusieurs comptes.'),
('Bank','Nom, code, description et logo ; liée aux comptes et profils.'),
('BankAccount','Relie client et banque ; numéro, devise, journal et compte comptable.'),
('BankStatementTemplate','Profil de lecture associé à une banque, parser et configuration.'),
('ExportTemplate / Field','Modèle et champs ordonnés avec source, format et caractère obligatoire.'),
('ConversionHistory','Auteur, client, compte, modèles facultatifs avant traitement, état, dates, erreurs et clés des fichiers.'),
('PendingFileDeletion','File persistante des clés de fichiers à supprimer.'),
('BankTransaction','Objet de traitement avec dates et montants ; aucun DbSet dédié aux opérations n’est déclaré.')],[150,349])
sub('Relations et contraintes')
text('Client 1 → N comptes ; Banque 1 → N comptes et profils ; Modèle d’export 1 → N champs ; Utilisateur / Client / Compte 1 → N historiques. Les clés sont des GUID. Les entités possèdent CreatedAt et UpdatedAt ; les mises à jour suivies par le DbContext actualisent UpdatedAt.')
text('Unicité : e-mail et nom utilisateur, ICE client, code banque, couple banque/numéro de compte et couple modèle/position de champ. Les relations sont restrictives, sauf la suppression en cascade des champs d’un modèle d’export. Les suppressions complètes orchestrent explicitement l’ordre de retrait des dépendances.')
sub('Évolutions de schéma présentes')
text('InitialCreate (22/09/2026), ClientProfilePhoto, BankLogo, ClientArchives et StagedDocuments (23/09/2026). Les profils et modèles de l’historique deviennent facultatifs pour permettre un import préalable au traitement. Les PDF et exports sont sur disque ; les photos et logos sont en base.')

page('16','API et contrats d’échange','S2 : contrôleurs HTTP ; S11 : client TypeScript.')
table(['Routes principales','Méthodes / comportement'],[
('/api/auth/login ; /register','POST : connexion ou création d’un User.'),
('/api/clients ; /bank-accounts','GET / POST ; GET, PUT, DELETE sur /{id}.'),
('/api/banks','CRUD ; GET /{id}/accounts et /{id}/profiles.'),
('/api/bank-statement-templates ; /export-templates','CRUD ; écritures réservées à Admin.'),
('/api/clients/{id}/photo ; /banks/{id}/logo','GET, PUT multipart et DELETE ; logo modifiable par Admin.'),
('/api/documents','POST multipart : importer et enregistrer en attente.'),
('/api/documents/{id}/process','POST avec profileId et exportId : traiter ou relancer.'),
('/api/documents/{id}','PUT multipart pour modifier ; DELETE pour supprimer.'),
('/api/conversions','POST multipart : import et traitement successifs.'),
('/api/conversions/{id}/download','GET : export du propriétaire ou accessible à Admin.'),
('/api/history ; /api/history/{id} ; /api/dashboard','GET : historique filtré, détail et indicateurs.'),
('/api/client-workspace/{clientId}/archives','GET ; sous-routes /{id}/preview/{kind} et /{id}/files/{kind}.'),
('/api/catalog-deletions/{banks|clients}/{id}','GET : impacts et version ; POST : confirmation avec version.'),
('/health ; /swagger','Santé API/base ; documentation selon la configuration.')],[265,234])
text('Les listes de référentiels et l’historique utilisent généralement {items, total, page, pageSize}, avec 100 éléments maximum par page. La liste d’archives a son propre contrat. Le client TypeScript transmet le Bearer et gère les erreurs. Les uploads utilisent le champ file ; les paramètres des documents et modèles sont transmis sous forme de champs ou JSON selon la route.')

page('17','Sécurité et robustesse constatées','S2 à S7, S10 et S11 ; analyse de code, sans audit de pénétration.')
sub('Mesures implémentées')
bullets(['JWT signé en HMAC-SHA256 ; validation de signature, émetteur, audience et durée de vie ; clé d’au moins 32 octets.', 'Autorisations serveur sur les mutations réservées à Admin ; rôle imposé à User lors de l’inscription.', 'Limitation de l’authentification à 20 requêtes par minute et par adresse IP ; limite de 2 conversions simultanées par nom utilisateur.', 'PDF : extension, MIME, signature %PDF- et taille réellement lue ; limite par défaut de 20 Mo.', 'Stockage sous noms aléatoires, contrôle du chemin résolu, absence de répertoire de fichiers exposé statiquement.', 'CORS limité aux origines configurées ; journaux structurés JSON et identifiant de trace pour les erreurs.', 'Contraintes relationnelles, opérations transactionnelles et protections contre certains traitements concurrents.'])
sub('Points à confirmer avant une utilisation élargie')
table(['Sujet','État / action à prévoir'],[
('Périmètre d’accès','Les documents et l’espace clients sont partagés. Faire valider cette règle par le cabinet ; elle ne correspond pas à une isolation par opérateur.'),
('Inscription','Endpoint ouvert. Définir invitation, validation ou restriction si l’accès doit être réservé aux membres du cabinet.'),
('Session navigateur','JWT dans sessionStorage ; renforcer la prévention XSS. Aucun renouvellement par refresh token observé.'),
('Conservation','Cinq réussites par client ; pas de conservation légale longue durée ni de journal d’audit immuable établi.'),
('Infrastructure','HTTPS, protection des secrets, sauvegardes, restauration et chiffrement au repos à organiser pour la production.')],[117,382])
text('Ces éléments sont des constats sur les mécanismes présents et les travaux de préparation. Ils ne constituent pas une preuve de conformité réglementaire ni un audit exhaustif de sécurité.')

page('18','Déploiement et exploitation','S1, S6, S8 et S12 : README, configuration et Compose.')
sub('Services et données persistantes')
text('Docker Compose décrit PostgreSQL, le backend et le frontend. Les ports publiés sont limités à localhost : interface sur 3001 et API sur 5080. Les volumes pgdata et uploads conservent la base et les documents. Le backend attend le contrôle de santé de PostgreSQL ; le frontend dépend du backend.')
table(['Configuration','Finalité'],[
('POSTGRES_PASSWORD / ConnectionStrings__Default','Identifiants PostgreSQL et chaîne de connexion.'),
('JWT_KEY / Jwt__Key ; issuer / audience','Signature et validation des sessions.'),
('ADMIN_EMAIL / ADMIN_PASSWORD','Création initiale du compte Admin ; aucun secret par défaut à utiliser.'),
('DEMO_DATA / Seed__DemoData','Ajout de référentiels fictifs. À désactiver après installation de la démonstration si nécessaire.'),
('NEXT_PUBLIC_API_URL / FRONTEND_ORIGIN','Adresse API compilée dans le frontend et origine CORS autorisée.'),
('Storage__Root / Storage__MaxBytes','Emplacement privé et limite de taille PDF.'),
('Database__AutoMigrate / Swagger__Enabled','Application des migrations et exposition de Swagger.'),
('Ocr__Script','Chemin du script OCR en lancement depuis les sources.')],[246,253])
sub('Démarrage et maintenance')
text('Copier le fichier d’exemple d’environnement, renseigner les secrets, puis lancer docker compose up --build -d. En local : PostgreSQL, variables .NET, dotnet restore, lancement de l’API, puis npm ci et npm run dev dans le frontend. Le fichier .env est lu par Compose ; il n’est pas chargé automatiquement par .NET.')
text('Les migrations peuvent être appliquées au démarrage ; pour plusieurs instances, prévoir une étape dédiée avant lancement. Le seed ne réinitialise pas les mots de passe existants. Une modification de NEXT_PUBLIC_API_URL nécessite une nouvelle compilation frontend. Sauvegarder conjointement la base et les fichiers pour préserver leur cohérence.')

page('19','Qualité et état d’avancement vérifiable','S1, S13 et exécution locale du 24/09/2026.')
table(['Périmètre','État et preuve'],[
('Backend : pipeline hors intégration','Vérifié dans cette session : 26 réussites, 0 échec. Extraction de fixture, montants, parsers, erreurs et exports selon les tests sélectionnés.'),
('Intégration PostgreSQL / API','Tests présents. Exécution non relancée pour ce rapport ; nécessite TEST_DATABASE_URL vers une base dédiée.'),
('PDF BMCE privés','Tests optionnels présents. Non relancés ici ; ils nécessitent les PDF et variables locales correspondantes.'),
('Frontend : build / TypeScript / lint','Réussites consignées dans le README lors des vérifications antérieures ; non relancées dans cette session.'),
('Interactions et rendu de l’application','Recette visuelle complète non établie pour ce rapport ; la documentation antérieure la laisse à réaliser.'),
('Docker','Configuration présente ; le rapport historique mentionne une syntaxe validée mais un moteur local en erreur. Déploiement complet non revérifié.'),
('Import dans Sage','Non certifié et non testé dans cette session ; recette métier requise.'),
('Fonctions récentes de suppression','Implémentation constatée ; une couverture automatisée exhaustive des variantes n’est pas établie.')],[160,339])
sub('Commande réellement exécutée')
text('dotnet test BankStatementConverter.sln --no-restore<br/>--filter "Category!=Integration&amp;Category!=LocalPdf"<br/>--verbosity minimal')
text('La commande a compilé les projets concernés et terminé avec 26 tests réussis. Le filtre exclut volontairement les tests d’intégration et les essais sur PDF locaux privés ; ce résultat ne valide donc pas tout le système.')
note('<b>Écart documentaire identifié :</b> des sections anciennes annoncent l’absence d’OCR ou de rétention automatique, alors que le code actuel inclut l’OCR BMCE et la rétention de cinq réussites. Le présent rapport privilégie les sources actuelles et signale les résultats historiques comme tels.')

page('20','Travaux restants et critères de livraison','Propositions issues des limites constatées ; pas un planning contractuel.')
table(['Priorité','Travail proposé','Critère de fin'],[
('P1 - Métier','Constituer un corpus anonymisé par banque et format ; contrôler scans, texte, pages multiples et cas limites.','Résultats attendus validés : opérations, dates, montants, soldes et libellés.'),
('P1 - Comptabilité','Valider les trois modèles avec l’installation Sage cible.','Fichiers importés sans erreur et écritures conformes au paramétrage métier.'),
('P1 - Recette web','Tester les formulaires, droits, photos/logos, thèmes, mobile, erreurs, reprise et suppression complète.','Parcours Admin et User signés dans un procès-verbal de recette.'),
('P1 - Exploitation','Valider Docker, HTTPS, accès, secrets, sauvegardes et restauration.','Déploiement reproductible et restauration démontrée en environnement de test.'),
('P1 - Accès / conservation','Faire confirmer le partage des documents et la rétention de cinq réussites.','Règles acceptées ou modifiées puis couvertes par des tests.'),
('P2 - Robustesse','Tester les interruptions serveur et la reprise des Processing ; étendre les tests de concurrence et de suppression.','Aucun état bloqué durablement dans les scénarios de panne définis.'),
('P2 - Documentation','Réconcilier README, guide OCR et rapport de vérification ; préciser versions Node et fonctions récentes.','Documentation cohérente avec le code livré.'),
('P3 - Évolutions','Étudier d’autres parsers bancaires, XLSX, gestion utilisateurs ou traitement asynchrone selon le besoin.','Spécifications et tests d’acceptation approuvés avant réalisation.')],[80,247,172])
text('L’effort restant ne peut pas être chiffré fiablement sans corpus bancaire cible, environnement Sage, exigences de conservation et capacité d’équipe. Ces priorités constituent une proposition de séquencement : validation fonctionnelle et exploitation d’abord, extensions ensuite.')

page('21','Annexe : sources et glossaire','Sources locales consultées ; aucun secret ni contenu de PDF privé reproduit.')
table(['Réf.','Fichiers / portée'],[
('S1','README.md : présentation, évolutions, limites et résultats antérieurs.'),
('S2','backend/BankStatementConverter.API/ : Program.cs et contrôleurs auth, catalogues, documents, historique, archives, images et suppressions.'),
('S3','Infrastructure/BankService.cs, CatalogService.cs, Validation.cs : règles de gestion et validation.'),
('S4','Infrastructure/ConversionService.cs : import, traitement, stockage, CSV standard et historique.'),
('S5','Infrastructure/Parsing.cs, Exporters.cs, BmceTextParser.cs, BmceOcr.cs : chaîne de conversion.'),
('S6','backend/ocr/README.md, bmce.cjs et package.json : OCR local et dépendances.'),
('S7','Infrastructure/ArchiveService.cs et CatalogDeletionService.cs : rétention et suppression.'),
('S8','Fichiers .csproj, Directory.Build.props, package.json, Dockerfiles et docker-compose.yml : technologies et déploiement.'),
('S9','frontend/Import-pdf-excel-master/src/app/ et src/components/ : pages, formulaires, session et revue des opérations.'),
('S10','Domain/Entities.cs, Infrastructure/AppDbContext.cs et Migrations/ : modèle et contraintes.'),
('S11','frontend/Import-pdf-excel-master/src/services/ : client API et chargement des données.'),
('S12','.env.example : variables à renseigner, sans valeurs secrètes.'),
('S13','docs/verification.md et backend/BankStatementConverter.Tests/ : vérifications documentées et tests disponibles.')],[40,459])
text('<b>Glossaire :</b> CRUD : créer, consulter, modifier, supprimer. DTO : objet d’échange API. JWT : jeton de session signé. OCR : reconnaissance de texte dans une image. Parser : lecteur spécialisé. Mapping : correspondance entre données et colonnes. Migration : évolution du schéma de base. Rétention : règle de conservation.')

class NumberedCanvas(canvas.Canvas):
    def __init__(self,*a,**k): super().__init__(*a,**k);self.states=[]
    def showPage(self): self.states.append(dict(self.__dict__));self._startPage()
    def save(self):
        total=len(self.states)
        for s in self.states:
            self.__dict__.update(s)
            self.setStrokeColor(colors.HexColor('#D5E2EC'));self.line(48,47,547,47)
            self.setFillColor(GRAY);self.setFont('Report',7.5)
            self.drawString(48,32,'RELEVEFLOW  •  Rapport d’avancement  •  24 septembre 2026')
            self.drawRightString(547,32,f'{self._pageNumber} / {total}')
            if self._pageNumber>1:
                self.setFillColor(BLUE);self.rect(0,814,595,28,fill=1,stroke=0)
                self.setFillColor(colors.white);self.setFont('ReportBold',8);self.drawString(48,824,'APPLICATION DE CONVERSION DES RELEVÉS BANCAIRES')
            super().showPage()
        super().save()

doc=SimpleDocTemplate(str(OUT),pagesize=(595,842),rightMargin=48,leftMargin=48,topMargin=52,bottomMargin=64,title='ReleveFlow - Rapport détaillé d’avancement',author='Documentation du projet ReleveFlow',subject='Fonctionnalités, scénarios, architecture et état d’avancement')
doc.build(story,canvasmaker=NumberedCanvas)
print(OUT)
