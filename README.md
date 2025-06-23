# BudgetBuilder
Front: https://github.com/Sauliusm1/BudgetBuilderReact

T120B165 Saityno taikomųjų programų projektavimas projektas

1.	Sprendžiamo uždavinio aprašymas

1.1.	Sistemos paskirtis
Projekto tikslas – suteikti galimybę įmonėms sekti savo pajamas, išlaidas ir parengti su savo biudžetu susijusias ataskaitas.
Veikimo principas – pačią kuriamą sistemą sudaro dvi dalys: internetinė aplikacija, kuria naudosis vaistininkai, administratorius bei aplikacijų programavimo sąsaja (angl. trump. API).
Darbuotojas, norėdamas naudotis šia sistema, prisiregistruos prie internetinės aplikacijos ir ten galės įvesti duomenis į biudžetą įplaukiančias pajamas, bei pateikti informaciją apie atliktus pirkimus. Šią informaciją galės patvirtinti tam skirtas įmonės vadybininkas. Darbuotojas gali matyti bei sukurti ataskaitą. Administratorius tvirtintų įmonių bei jų darbuotojų registracijas. 

1.2.	Funkciniai reikalavimai
Neregistruotas naudotojas galės prisiregistruoti prie sistemos.
Registruotas naudotojas galės prisijungti/atsijungti nuo sistemos; pateikti duomenis apie pirkimą patvirtinimui; pateikti duomenis apie įplaukas į biudžetą patirtinimui; redaguoti savo pateiktus įrašus(reikės atskiro patvirtinimo); peržiūrėti detalų išlaidų ir pajamų sąrašą; generuoti ataskaitas.
Vadybininkas turės visas registruoto naudotojo galimybes. Tačiau galės atlikti ir papildomų veiksmų: patvirtinti laukiančius duomenis; pridėti įvairių kategorijų detalesniam išlaidų bei įplaukų analizavimui; redaguoti visų įmonės įrašų informaciją; keisti savo įmonės darbuotojų paskyrų teises; 
Administratorius galės tvarkyti(patvirtinti bei šalinti) visų naudotojų paskyras; paskirti įmonės vadybininkus;	

3.	Sistemos architektūra
Kliento dalis – React.js
Serverio dalis – .NET, duomenų bazė MySQL
1 pav. pavaizduota numatoma kuriamos sistemos diegimo diagrama. Sistemos talpinama į serverį. Internetinė aplikacija pasiekiama naudojant HTTPS protokolą. Sistemos veikimui yra reikalingas „BudegtBuilder“ API, kuris pasiekiamas per internetinės aplikacijos sąsają arba tiesiogiai naudojant TCP/IP protokolą. Minėta API sąsaja gali pasiekti MySQL duomenų baze.
 1 pav. Sistemos „BudgetBuilder“ diegimo diagrama
