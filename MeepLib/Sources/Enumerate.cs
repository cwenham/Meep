using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using SmartFormat;

using MeepLib.Config;
using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Sources
{
    /// <summary>
    /// Enumerate list items, including paragraphs or words of built-in libraries of plain text (including Lorem Ipsum and country names)
    /// </summary>
    /// <remarks>Provide your own collections of items with <see cref="MeepLib.Config.Text"/> elements, or name a built-in or
    /// plugin-supplied list with the Selection attribute.
    /// 
    /// <para>Ideal combined with emitters and modifiers of Step messages such as Timer and Random. Use Lorem Ipsum 
    /// for testing text interfaces, or the bad password list for automated pen testing. Having a few bog-standard 
    /// common texts hard-coded into a message pipeline library like Meep can be very convenient for quick jobs without 
    /// needing to source a text file from somewhere. Otherwise, you can still easily consume a text file with the Load
    /// module and parse/split it with the Extract and Split modules.</para></remarks>
    public class Enumerate : AMessageModule
    {
        public Enumerate() : base()
        { }

        public Enumerate(string selection) : base()
        {
            Selection = selection;
        }

        public List<DataSelector> Items
        {
            get
            {
                if (_items is null)
                    _items = (from i in this.Config.OfType<Text>()
                              select new DataSelector(i.Content)).ToList();

                return _items;
            }
            private set
            {
                _items = value;
            }
        }
        private List<DataSelector> _items;

        /// <summary>
        /// Name of the 'book' in the library
        /// </summary>
        /// <remarks>Leave empty if you're supplying a list with <see cref="MeepLib.Config.Text"/> elements.</remarks>
        public string Selection
        {
            get
            {
                return _selection;
            }
            set
            {
                _selection = value;

                var selections = GetSelections();
                if (selections.ContainsKey(_selection))
                    _book = selections[_selection].Select(x => new DataSelector(x)).ToList();
                else
                    throw new ArgumentException($"{_selection} book not found. Check for misspelling or that its containing plugin has been loaded.");
            }
        }
        private string _selection;

        private List<DataSelector> _book;

        /// <summary>
        /// Index number in {Smart.Format}
        /// </summary>
        /// <remarks>This will be wrapped around rather than throw an out-of-range exception, so 12 on a 5-item
        /// list will evaluate to item 2.</remarks>
        public DataSelector Index { get; set; } = "{msg.Number}";

        /// <summary>
        /// List of 'books' in the library
        /// </summary>
        /// <returns></returns>
        /// <remarks>Plugins can add books to the library by defining each of them as a static List:lt;string&gt; of
        /// paragraphs in a static class and decorated with the [LibraryBook] attribute. We will discover them by 
        /// reflection after the plugin is loaded.
        /// 
        /// <para>It has to be a List&lt;string&gt; for performance, since we will need to check its length to
        /// support wraparound indexing.</para>
        /// </remarks>
        public static Dictionary<string,List<string>> GetSelections()
        {
            var staticClasses = from a in AppDomain.CurrentDomain.GetAssemblies()
                                from t in a.GetTypes()
                                where t.IsAbstract && t.IsSealed
                                select t;

            var staticLists = from t in staticClasses
                              from f in t.GetFields()
                              where f.FieldType == typeof(List<string>)
                                 && f.IsStatic
                              let bookAttrib = f.GetCustomAttributes(typeof(LibraryBookAttribute), true).FirstOrDefault()
                              where bookAttrib != null
                              let book = f.GetValue(null) as List<string>
                              where book != null
                              select new 
                              {
                                  Book = book,
                                  Attrib = bookAttrib as LibraryBookAttribute
                              };

            return staticLists.ToDictionary(x => x.Attrib.Name, y => y.Book);
        }

        public async override Task<Message> HandleMessage(Message msg)
        {
            if (msg is null)
                return null;

            if (_book is null)
                // Wasn't set earlier in deserialisation? Okay, maybe we're in <Text> list mode
                if (Items != null)
                    _book = Items;

            if (_book is null || _book.Count == 0)
                return null;

            MessageContext context = new MessageContext(msg, this);

            try
            {
                (bool parsed, long dsIndex) = await Index.TrySelectLongAsync(context);
                if (!parsed)
                    return null;

                if (dsIndex > _book.Count - 1)
                    dsIndex = dsIndex % (_book.Count);

                return new StringMessage
                {
                    DerivedFrom = msg,
                    Value = await _book[(int)dsIndex].SelectStringAsync(context)
                };
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "{0} thrown when enumerating from {1}: {2}", ex.GetType().Name, Selection, ex.Message);
                return null;
            }
        }
    }

    [System.AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class LibraryBookAttribute : Attribute
    {
        readonly string name;

        public LibraryBookAttribute(string name)
        {
            this.name = name;
        }

        public string Name
        {
            get { return name; }
        }
    }

    /// <summary>
    /// Library of bog-standard public domain texts used for testing, security analysis, etc.
    /// </summary>
    public static class BaseTextLibrary
    {
        /// <summary>
        /// Lorem ipsum (6 paragraphs worth)
        /// </summary>
        [LibraryBook("Lorem Ipsum")]
        public static List<string> LoremIpsum = new List<string>
        {
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Ut faucibus elit ante, id porta diam aliquet at. " +
            "Pellentesque diam sem, convallis quis maximus in, tincidunt in augue. In venenatis vel orci eu tempus. " +
            "Vivamus molestie dui pharetra leo scelerisque, gravida sodales tellus cursus. Quisque egestas nunc at " +
            "tellus vestibulum, id vulputate risus viverra. Fusce aliquet lacus sed pharetra suscipit. Duis malesuada " +
            "dolor et mauris imperdiet maximus. In eget dolor vitae dui elementum sodales. Donec bibendum lacinia sem, " +
            "egestas volutpat nisl scelerisque sed. Phasellus auctor suscipit hendrerit. Sed pretium dui odio, at " +
            "ornare quam fermentum ut. Nullam vel tincidunt arcu, non efficitur lectus. Proin nec volutpat nunc. Ut at " +
            "enim eros. Mauris arcu nunc, semper non pharetra vitae, fringilla ac nunc. Vivamus posuere diam vel nibh " +
            "ultrices, ut sagittis enim feugiat.",

            "Etiam quis diam tempor, suscipit lorem id, sagittis dolor. Integer sollicitudin dictum purus, et porttitor " +
            "turpis dignissim quis. Sed fringilla sed sem id sodales. Nullam quis malesuada tortor. Pellentesque " +
            "fermentum tincidunt lacus ut posuere. Sed et lacus varius, porttitor lacus a, venenatis ligula. " +
            "Pellentesque mollis congue pulvinar. Suspendisse maximus aliquet leo vel cursus. Praesent in feugiat " +
            "justo. Sed tempor tempor sapien, vitae feugiat lorem mollis at. Pellentesque habitant morbi tristique " +
            "senectus et netus et malesuada fames ac turpis egestas. Interdum et malesuada fames ac ante ipsum primis " +
            "in faucibus. Integer viverra vehicula neque ut consectetur. Proin at ultricies turpis. Aliquam ut ex in " +
            "leo eleifend rhoncus. Donec convallis ante tincidunt metus varius faucibus.",

            "Cras sollicitudin bibendum ipsum ac viverra. Cras ut commodo sapien. Sed tincidunt lobortis justo. In " +
            "sollicitudin vestibulum lorem, eu luctus sem mattis a. Nullam condimentum auctor tellus, ut semper leo " +
            "tristique et. Maecenas a dolor viverra, aliquet enim fringilla, eleifend metus. Pellentesque elementum " +
            "elementum felis ut ornare. Aliquam consectetur, velit at laoreet dignissim, dolor lectus lacinia sem, at " +
            "vehicula risus lorem a velit. Integer rutrum fringilla elit, non condimentum elit placerat nec. Integer " +
            "massa nunc, vestibulum vel felis nec, euismod scelerisque ante. Sed eget arcu odio. Etiam in arcu arcu.",

            "Donec eget ex eget neque commodo posuere non eget ex. Donec a est enim. Praesent vitae lectus bibendum, " +
            "dictum ligula ut, finibus sem. In finibus fermentum nulla in cursus. Praesent ac vehicula ex. Fusce " +
            "sagittis vulputate augue, vel rhoncus elit tincidunt luctus. Sed viverra mauris sed ultrices iaculis. " +
            "Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Nunc iaculis " +
            "nec diam nec ultrices. Nullam vitae libero at libero fermentum lacinia. Cras aliquet sagittis sem nec " +
            "tristique. Proin ultricies ligula ac tortor suscipit tempus. Nulla gravida, massa ac lobortis feugiat, " +
            "felis libero vehicula nunc, et porttitor metus magna ut sapien. Integer ac ultricies quam. Aliquam vitae " +
            "libero non diam iaculis aliquet vel et nulla.",

            "Suspendisse id volutpat nunc, cursus tempus nibh. Fusce consectetur pulvinar velit, eu faucibus ex " +
            "volutpat eu. Fusce nec libero quis augue feugiat viverra. Curabitur egestas sollicitudin hendrerit. " +
            "Aliquam tempor sagittis dui, non placerat orci fringilla sed. Etiam lacinia dictum mollis. Vestibulum " +
            "elementum viverra fermentum. Sed mattis commodo quam, non interdum lectus faucibus eu. Aliquam at ultrices " +
            "lorem.",

            "Donec et maximus purus. Nullam ut ligula at erat vehicula porttitor vel vitae nulla. Quisque at bibendum " +
            "magna. Phasellus faucibus enim sed purus tristique, ac accumsan lacus scelerisque. Nullam auctor quam dui, " +
            "non faucibus ligula pharetra eu. Vivamus gravida feugiat semper. Etiam sit amet orci ligula. Duis quis " +
            "nulla efficitur, varius est ac, auctor felis."
        };

        /// <summary>
        /// Top 1000 common passwords
        /// </summary>
        /// <remarks>From https://github.com/danielmiessler/SecLists/blob/master/Passwords/Common-Credentials/10-million-password-list-top-1000.txt
        /// <para>Use for automated pen testing or policy enforcement.</para>
        /// </remarks>
        [LibraryBook("Common Passwords")]
        public static List<string> CommonPasswords = new List<string>
        {
            "123456","password","12345678","qwerty","123456789","12345","1234","111111","1234567","dragon","123123",
            "baseball","abc123","football","monkey","letmein","696969","shadow","master","666666","qwertyuiop",
            "123321","mustang","1234567890","michael","654321","pussy","superman","1qaz2wsx","7777777","fuckyou",
            "121212","000000","qazwsx","123qwe","killer","trustno1","jordan","jennifer","zxcvbnm","asdfgh",
            "hunter","buster","soccer","harley","batman","andrew","tigger","sunshine","iloveyou","fuckme",
            "2000","charlie","robert","thomas","hockey","ranger","daniel","starwars","klaster","112233",
            "george","asshole","computer","michelle","jessica","pepper","1111","zxcvbn","555555","11111111",
            "131313","freedom","777777","pass","fuck","maggie","159753","aaaaaa","ginger","princess",
            "joshua","cheese","amanda","summer","love","ashley","6969","nicole","chelsea","biteme",
            "matthew","access","yankees","987654321","dallas","austin","thunder","taylor","matrix","william",
            "corvette","hello","martin","heather","secret","fucker","merlin","diamond","1234qwer","gfhjkm",
            "hammer","silver","222222","88888888","anthony","justin","test","bailey","q1w2e3r4t5","patrick",
            "internet","scooter","orange","11111","golfer","cookie","richard","samantha","bigdog","guitar",
            "jackson","whatever","mickey","chicken","sparky","snoopy","maverick","phoenix","camaro","sexy",
            "peanut","morgan","welcome","falcon","cowboy","ferrari","samsung","andrea","smokey","steelers",
            "joseph","mercedes","dakota","arsenal","eagles","melissa","boomer","booboo","spider","nascar",
            "monster","tigers","yellow","xxxxxx","123123123","gateway","marina","diablo","bulldog","qwer1234",
            "compaq","purple","hardcore","banana","junior","hannah","123654","porsche","lakers","iceman",
            "money","cowboys","987654","london","tennis","999999","ncc1701","coffee","scooby","0000",
            "miller","boston","q1w2e3r4","fuckoff","brandon","yamaha","chester","mother","forever","johnny",
            "edward","333333","oliver","redsox","player","nikita","knight","fender","barney","midnight",
            "please","brandy","chicago","badboy","iwantu","slayer","rangers","charles","angel","flower",
            "bigdaddy","rabbit","wizard","bigdick","jasper","enter","rachel","chris","steven","winner",
            "adidas","victoria","natasha","1q2w3e4r","jasmine","winter","prince","panties","marine","ghbdtn",
            "fishing","cocacola","casper","james","232323","raiders","888888","marlboro","gandalf","asdfasdf",
            "crystal","87654321","12344321","sexsex","golden","blowme","bigtits","8675309","panther","lauren",
            "angela","bitch","spanky","thx1138","angels","madison","winston","shannon","mike","toyota",
            "blowjob","jordan23","canada","sophie","Password","apples","dick","tiger","razz","123abc",
            "pokemon","qazxsw","55555","qwaszx","muffin","johnson","murphy","cooper","jonathan","liverpoo",
            "david","danielle","159357","jackie","1990","123456a","789456","turtle","horny","abcd1234",
            "scorpion","qazwsxedc","101010","butter","carlos","password1","dennis","slipknot","qwerty123","booger",
            "asdf","1991","black","startrek","12341234","cameron","newyork","rainbow","nathan","john",
            "1992","rocket","viking","redskins","butthead","asdfghjkl","1212","sierra","peaches","gemini",
            "doctor","wilson","sandra","helpme","qwertyui","victor","florida","dolphin","pookie","captain",
            "tucker","blue","liverpool","theman","bandit","dolphins","maddog","packers","jaguar","lovers",
            "nicholas","united","tiffany","maxwell","zzzzzz","nirvana","jeremy","suckit","stupid","porn",
            "monica","elephant","giants","jackass","hotdog","rosebud","success","debbie","mountain","444444",
            "xxxxxxxx","warrior","1q2w3e4r5t","q1w2e3","123456q","albert","metallic","lucky","azerty","7777",
            "shithead","alex","bond007","alexis","1111111","samson","5150","willie","scorpio","bonnie",
            "gators","benjamin","voodoo","driver","dexter","2112","jason","calvin","freddy","212121",
            "creative","12345a","sydney","rush2112","1989","asdfghjk","red123","bubba","4815162342","passw0rd",
            "trouble","gunner","happy","fucking","gordon","legend","jessie","stella","qwert","eminem",
            "arthur","apple","nissan","bullshit","bear","america","1qazxsw2","nothing","parker","4444",
            "rebecca","qweqwe","garfield","01012011","beavis","69696969","jack","asdasd","december","2222",
            "102030","252525","11223344","magic","apollo","skippy","315475","girls","kitten","golf",
            "copper","braves","shelby","godzilla","beaver","fred","tomcat","august","buddy","airborne",
            "1993","1988","lifehack","qqqqqq","brooklyn","animal","platinum","phantom","online","xavier",
            "darkness","blink182","power","fish","green","789456123","voyager","police","travis","12qwaszx",
            "heaven","snowball","lover","abcdef","00000","pakistan","007007","walter","playboy","blazer",
            "cricket","sniper","hooters","donkey","willow","loveme","saturn","therock","redwings","bigboy",
            "pumpkin","trinity","williams","tits","nintendo","digital","destiny","topgun","runner","marvin",
            "guinness","chance","bubbles","testing","fire","november","minecraft","asdf1234","lasvegas","sergey",
            "broncos","cartman","private","celtic","birdie","little","cassie","babygirl","donald","beatles",
            "1313","dickhead","family","12121212","school","louise","gabriel","eclipse","fluffy","147258369",
            "lol123","explorer","beer","nelson","flyers","spencer","scott","lovely","gibson","doggie",
            "cherry","andrey","snickers","buffalo","pantera","metallica","member","carter","qwertyu","peter",
            "alexande","steve","bronco","paradise","goober","5555","samuel","montana","mexico","dreams",
            "michigan","cock","carolina","yankee","friends","magnum","surfer","poopoo","maximus","genius",
            "cool","vampire","lacrosse","asd123","aaaa","christin","kimberly","speedy","sharon","carmen",
            "111222","kristina","sammy","racing","ou812","sabrina","horses","0987654321","qwerty1","pimpin",
            "baby","stalker","enigma","147147","star","poohbear","boobies","147258","simple","bollocks",
            "12345q","marcus","brian","1987","qweasdzxc","drowssap","hahaha","caroline","barbara","dave",
            "viper","drummer","action","einstein","bitches","genesis","hello1","scotty","friend","forest",
            "010203","hotrod","google","vanessa","spitfire","badger","maryjane","friday","alaska","1232323q",
            "tester","jester","jake","champion","billy","147852","rock","hawaii","badass","chevy",
            "420420","walker","stephen","eagle1","bill","1986","october","gregory","svetlana","pamela",
            "1984","music","shorty","westside","stanley","diesel","courtney","242424","kevin","porno",
            "hitman","boobs","mark","12345qwert","reddog","frank","qwe123","popcorn","patricia","aaaaaaaa",
            "1969","teresa","mozart","buddha","anderson","paul","melanie","abcdefg","security","lucky1",
            "lizard","denise","3333","a12345","123789","ruslan","stargate","simpsons","scarface","eagle",
            "123456789a","thumper","olivia","naruto","1234554321","general","cherokee","a123456","vincent","Usuckballz1",
            "spooky","qweasd","cumshot","free","frankie","douglas","death","1980","loveyou","kitty",
            "kelly","veronica","suzuki","semperfi","penguin","mercury","liberty","spirit","scotland","natalie",
            "marley","vikings","system","sucker","king","allison","marshall","1979","098765","qwerty12",
            "hummer","adrian","1985","vfhbyf","sandman","rocky","leslie","antonio","98765432","4321",
            "softball","passion","mnbvcxz","bastard","passport","horney","rascal","howard","franklin","bigred",
            "assman","alexander","homer","redrum","jupiter","claudia","55555555","141414","zaq12wsx","shit",
            "patches","nigger","cunt","raider","infinity","andre","54321","galore","college","russia",
            "kawasaki","bishop","77777777","vladimir","money1","freeuser","wildcats","francis","disney","budlight",
            "brittany","1994","00000000","sweet","oksana","honda","domino","bulldogs","brutus","swordfis",
            "norman","monday","jimmy","ironman","ford","fantasy","9999","7654321","PASSWORD","hentai",
            "duncan","cougar","1977","jeffrey","house","dancer","brooke","timothy","super","marines",
            "justice","digger","connor","patriots","karina","202020","molly","everton","tinker","alicia",
            "rasdzv3","poop","pearljam","stinky","naughty","colorado","123123a","water","test123","ncc1701d",
            "motorola","ireland","asdfg","slut","matt","houston","boogie","zombie","accord","vision",
            "bradley","reggie","kermit","froggy","ducati","avalon","6666","9379992","sarah","saints",
            "logitech","chopper","852456","simpson","madonna","juventus","claire","159951","zachary","yfnfif",
            "wolverin","warcraft","hello123","extreme","penis","peekaboo","fireman","eugene","brenda","123654789",
            "russell","panthers","georgia","smith","skyline","jesus","elizabet","spiderma","smooth","pirate",
            "empire","bullet","8888","virginia","valentin","psycho","predator","arizona","134679","mitchell",
            "alyssa","vegeta","titanic","christ","goblue","fylhtq","wolf","mmmmmm","kirill","indian",
            "hiphop","baxter","awesome","people","danger","roland","mookie","741852963","1111111111","dreamer",
            "bambam","arnold","1981","skipper","serega","rolltide","elvis","changeme","simon","1q2w3e",
            "lovelove","fktrcfylh","denver","tommy","mine","loverboy","hobbes","happy1","alison","nemesis",
            "chevelle","cardinal","burton","wanker","picard","151515","tweety","michael1","147852369","12312",
            "xxxx","windows","turkey","456789","1974","vfrcbv","sublime","1975","galina","bobby",
            "newport","manutd","daddy","american","alexandr","1966","victory","rooster","qqq111","madmax",
            "electric","bigcock","a1b2c3","wolfpack","spring","phpbb","lalala","suckme","spiderman","eric",
            "darkside","classic","raptor","123456789q","hendrix","1982","wombat","avatar","alpha","zxc123",
            "crazy","hard","england","brazil","1978","01011980","wildcat","polina","freepass"
        };

        /// <summary>
        /// Top 1000 most common last names in the US
        /// </summary>
        /// <remarks>Combined with random first initial to discover user accounts during pen testing, or
        /// discovering sock-puppet farming patterns.</remarks>
        [LibraryBook("Common Last Names")]
        public static List<string> CommonLastNames = new List<string>
        {
            "Smith","Johnson","Williams","Jones","Brown","Davis","Miller","Wilson","Moore","Taylor","Anderson",
            "Thomas","Jackson","White","Harris","Martin","Thompson","Garcia","Martinez","Robinson","Clark",
            "Rodriguez","Lewis","Lee","Walker","Hall","Allen","Young","Hernandez","King","Wright",
            "Lopez","Hill","Scott","Green","Adams","Baker","Gonzalez","Nelson","Carter","Mitchell",
            "Perez","Roberts","Turner","Phillips","Campbell","Parker","Evans","Edwards","Collins","Stewart",
            "Sanchez","Morris","Rogers","Reed","Cook","Morgan","Bell","Murphy","Bailey","Rivera",
            "Cooper","Richardson","Cox","Howard","Ward","Torres","Peterson","Gray","Ramirez","James",
            "Watson","Brooks","Kelly","Sanders","Price","Bennett","Wood","Barnes","Ross","Henderson",
            "Coleman","Jenkins","Perry","Powell","Long","Patterson","Hughes","Flores","Washington","Butler",
            "Simmons","Foster","Gonzales","Bryant","Alexander","Russell","Griffin","Diaz","Hayes","Myers",
            "Ford","Hamilton","Graham","Sullivan","Wallace","Woods","Cole","West","Jordan","Owens",
            "Reynolds","Fisher","Ellis","Harrison","Gibson","Mcdonald","Cruz","Marshall","Ortiz","Gomez",
            "Murray","Freeman","Wells","Webb","Simpson","Stevens","Tucker","Porter","Hunter","Hicks",
            "Crawford","Henry","Boyd","Mason","Morales","Kennedy","Warren","Dixon","Ramos","Reyes",
            "Burns","Gordon","Shaw","Holmes","Rice","Robertson","Hunt","Black","Daniels","Palmer",
            "Mills","Nichols","Grant","Knight","Ferguson","Rose","Stone","Hawkins","Dunn","Perkins",
            "Hudson","Spencer","Gardner","Stephens","Payne","Pierce","Berry","Matthews","Arnold","Wagner",
            "Willis","Ray","Watkins","Olson","Carroll","Duncan","Snyder","Hart","Cunningham","Bradley",
            "Lane","Andrews","Ruiz","Harper","Fox","Riley","Armstrong","Carpenter","Weaver","Greene",
            "Lawrence","Elliott","Chavez","Sims","Austin","Peters","Kelley","Franklin","Lawson","Fields",
            "Gutierrez","Ryan","Schmidt","Carr","Vasquez","Castillo","Wheeler","Chapman","Oliver","Montgomery",
            "Richards","Williamson","Johnston","Banks","Meyer","Bishop","Mccoy","Howell","Alvarez","Morrison",
            "Hansen","Fernandez","Garza","Harvey","Little","Burton","Stanley","Nguyen","George","Jacobs",
            "Reid","Kim","Fuller","Lynch","Dean","Gilbert","Garrett","Romero","Welch","Larson",
            "Frazier","Burke","Hanson","Day","Mendoza","Moreno","Bowman","Medina","Fowler","Brewer",
            "Hoffman","Carlson","Silva","Pearson","Holland","Douglas","Fleming","Jensen","Vargas","Byrd",
            "Davidson","Hopkins","May","Terry","Herrera","Wade","Soto","Walters","Curtis","Neal",
            "Caldwell","Lowe","Jennings","Barnett","Graves","Jimenez","Horton","Shelton","Barrett","Obrien",
            "Castro","Sutton","Gregory","Mckinney","Lucas","Miles","Craig","Rodriquez","Chambers","Holt",
            "Lambert","Fletcher","Watts","Bates","Hale","Rhodes","Pena","Beck","Newman","Haynes",
            "Mcdaniel","Mendez","Bush","Vaughn","Parks","Dawson","Santiago","Norris","Hardy","Love",
            "Steele","Curry","Powers","Schultz","Barker","Guzman","Page","Munoz","Ball","Keller",
            "Chandler","Weber","Leonard","Walsh","Lyons","Ramsey","Wolfe","Schneider","Mullins","Benson",
            "Sharp","Bowen","Daniel","Barber","Cummings","Hines","Baldwin","Griffith","Valdez","Hubbard",
            "Salazar","Reeves","Warner","Stevenson","Burgess","Santos","Tate","Cross","Garner","Mann",
            "Mack","Moss","Thornton","Dennis","Mcgee","Farmer","Delgado","Aguilar","Vega","Glover",
            "Manning","Cohen","Harmon","Rodgers","Robbins","Newton","Todd","Blair","Higgins","Ingram",
            "Reese","Cannon","Strickland","Townsend","Potter","Goodwin","Walton","Rowe","Hampton","Ortega",
            "Patton","Swanson","Joseph","Francis","Goodman","Maldonado","Yates","Becker","Erickson","Hodges",
            "Rios","Conner","Adkins","Webster","Norman","Malone","Hammond","Flowers","Cobb","Moody",
            "Quinn","Blake","Maxwell","Pope","Floyd","Osborne","Paul","Mccarthy","Guerrero","Lindsey",
            "Estrada","Sandoval","Gibbs","Tyler","Gross","Fitzgerald","Stokes","Doyle","Sherman","Saunders",
            "Wise","Colon","Gill","Alvarado","Greer","Padilla","Simon","Waters","Nunez","Ballard",
            "Schwartz","Mcbride","Houston","Christensen","Klein","Pratt","Briggs","Parsons","Mclaughlin","Zimmerman",
            "French","Buchanan","Moran","Copeland","Roy","Pittman","Brady","Mccormick","Holloway","Brock",
            "Poole","Frank","Logan","Owen","Bass","Marsh","Drake","Wong","Jefferson","Park",
            "Morton","Abbott","Sparks","Patrick","Norton","Huff","Clayton","Massey","Lloyd","Figueroa",
            "Carson","Bowers","Roberson","Barton","Tran","Lamb","Harrington","Casey","Boone","Cortez",
            "Clarke","Mathis","Singleton","Wilkins","Cain","Bryan","Underwood","Hogan","Mckenzie","Collier",
            "Luna","Phelps","Mcguire","Allison","Bridges","Wilkerson","Nash","Summers","Atkins","Wilcox",
            "Pitts","Conley","Marquez","Burnett","Richard","Cochran","Chase","Davenport","Hood","Gates",
            "Clay","Ayala","Sawyer","Roman","Vazquez","Dickerson","Hodge","Acosta","Flynn","Espinoza",
            "Nicholson","Monroe","Wolf","Morrow","Kirk","Randall","Anthony","Whitaker","Oconnor","Skinner",
            "Ware","Molina","Kirby","Huffman","Bradford","Charles","Gilmore","Dominguez","Oneal","Bruce",
            "Lang","Combs","Kramer","Heath","Hancock","Gallagher","Gaines","Shaffer","Short","Wiggins",
            "Mathews","Mcclain","Fischer","Wall","Small","Melton","Hensley","Bond","Dyer","Cameron",
            "Grimes","Contreras","Christian","Wyatt","Baxter","Snow","Mosley","Shepherd","Larsen","Hoover",
            "Beasley","Glenn","Petersen","Whitehead","Meyers","Keith","Garrison","Vincent","Shields","Horn",
            "Savage","Olsen","Schroeder","Hartman","Woodard","Mueller","Kemp","Deleon","Booth","Patel",
            "Calhoun","Wiley","Eaton","Cline","Navarro","Harrell","Lester","Humphrey","Parrish","Duran",
            "Hutchinson","Hess","Dorsey","Bullock","Robles","Beard","Dalton","Avila","Vance","Rich",
            "Blackwell","York","Johns","Blankenship","Trevino","Salinas","Campos","Pruitt","Moses","Callahan",
            "Golden","Montoya","Hardin","Guerra","Mcdowell","Carey","Stafford","Gallegos","Henson","Wilkinson",
            "Booker","Merritt","Miranda","Atkinson","Orr","Decker","Hobbs","Preston","Tanner","Knox",
            "Pacheco","Stephenson","Glass","Rojas","Serrano","Marks","Hickman","English","Sweeney","Strong",
            "Prince","Mcclure","Conway","Walter","Roth","Maynard","Farrell","Lowery","Hurst","Nixon",
            "Weiss","Trujillo","Ellison","Sloan","Juarez","Winters","Mclean","Randolph","Leon","Boyer",
            "Villarreal","Mccall","Gentry","Carrillo","Kent","Ayers","Lara","Shannon","Sexton","Pace",
            "Hull","Leblanc","Browning","Velasquez","Leach","Chang","House","Sellers","Herring","Noble",
            "Foley","Bartlett","Mercado","Landry","Durham","Walls","Barr","Mckee","Bauer","Rivers",
            "Everett","Bradshaw","Pugh","Velez","Rush","Estes","Dodson","Morse","Sheppard","Weeks",
            "Camacho","Bean","Barron","Livingston","Middleton","Spears","Branch","Blevins","Chen","Kerr",
            "Mcconnell","Hatfield","Harding","Ashley","Solis","Herman","Frost","Giles","Blackburn","William",
            "Pennington","Woodward","Finley","Mcintosh","Koch","Best","Solomon","Mccullough","Dudley","Nolan",
            "Blanchard","Rivas","Brennan","Mejia","Kane","Benton","Joyce","Buckley","Haley","Valentine",
            "Maddox","Russo","Mcknight","Buck","Moon","Mcmillan","Crosby","Berg","Dotson","Mays",
            "Roach","Church","Chan","Richmond","Meadows","Faulkner","Oneill","Knapp","Kline","Barry",
            "Ochoa","Jacobson","Gay","Avery","Hendricks","Horne","Shepard","Hebert","Cherry","Cardenas",
            "Mcintyre","Whitney","Waller","Holman","Donaldson","Cantu","Terrell","Morin","Gillespie","Fuentes",
            "Tillman","Sanford","Bentley","Peck","Key","Salas","Rollins","Gamble","Dickson","Battle",
            "Santana","Cabrera","Cervantes","Howe","Hinton","Hurley","Spence","Zamora","Yang","Mcneil",
            "Suarez","Case","Petty","Gould","Mcfarland","Sampson","Carver","Bray","Rosario","Macdonald",
            "Stout","Hester","Melendez","Dillon","Farley","Hopper","Galloway","Potts","Bernard","Joyner",
            "Stein","Aguirre","Osborn","Mercer","Bender","Franco","Rowland","Sykes","Benjamin","Travis",
            "Pickett","Crane","Sears","Mayo","Dunlap","Hayden","Wilder","Mckay","Coffey","Mccarty",
            "Ewing","Cooley","Vaughan","Bonner","Cotton","Holder","Stark","Ferrell","Cantrell","Fulton",
            "Lynn","Lott","Calderon","Rosa","Pollard","Hooper","Burch","Mullen","Fry","Riddle",
            "Levy","David","Duke","Odonnell","Guy","Michael","Britt","Frederick","Daugherty","Berger",
            "Dillard","Alston","Jarvis","Frye","Riggs","Chaney","Odom","Duffy","Fitzpatrick","Valenzuela",
            "Merrill","Mayer","Alford","Mcpherson","Acevedo","Donovan","Barrera","Albert","Cote","Reilly",
            "Compton","Raymond","Mooney","Mcgowan","Craft","Cleveland","Clemons","Wynn","Nielsen","Baird",
            "Stanton","Snider","Rosales","Bright","Witt","Stuart","Hays","Holden","Rutledge","Kinney",
            "Clements","Castaneda","Slater","Hahn","Emerson","Conrad","Burks","Delaney","Pate","Lancaster",
            "Sweet","Justice","Tyson","Sharpe","Whitfield","Talley","Macias","Irwin","Burris","Ratliff",
            "Mccray","Madden","Kaufman","Beach","Goff","Cash","Bolton","Mcfadden","Levine","Good",
            "Byers","Kirkland","Kidd","Workman","Carney","Dale","Mcleod","Holcomb","England","Finch",
            "Head","Burt","Hendrix","Sosa","Haney","Franks","Sargent","Nieves","Downs","Rasmussen",
            "Bird","Hewitt","Lindsay","Le","Foreman","Valencia","Oneil","Delacruz","Vinson","Dejesus",
            "Hyde","Forbes","Gilliam","Guthrie","Wooten","Huber","Barlow","Boyle","Mcmahon","Buckner",
            "Rocha","Puckett","Langley","Knowles","Cooke","Velazquez","Whitley","Noel","Vang",
        };

        /// <summary>
        /// List of countries that existed at the time of writing
        /// </summary>
        [LibraryBook("Countries")]
        public static List<string> Countries = new List<string>
        {
            "Afghanistan","Albania","Algeria","Andorra","Angola","Antigua and Barbuda",
            "Argentina","Armenia","Australia","Austria","Azerbaijan",
            "The Bahamas","Bahrain","Bangladesh","Barbados","Belarus",
            "Belgium","Belize","Benin","Bhutan","Bolivia",
            "Bosnia and Herzegovina","Botswana","Brazil","Brunei","Bulgaria",
            "Burkina Faso","Burundi","Cambodia","Cameroon","Canada",
            "Cape Verde","Central African Republic","Chad","Chile","China",
            "Colombia","Comoros","Congo, Republic of the","Congo, Democratic Republic of the","Costa Rica",
            "Cote d’Ivoire","Croatia","Cuba","Cyprus","Czech Republic",
            "Denmark","Djibouti","Dominica","Dominican Republic","East Timor (Timor-Leste)",
            "Ecuador","Egypt","El Salvador","Equatorial Guinea","Eritrea",
            "Estonia","Ethiopia","Fiji","Finland","France",
            "Gabon","The Gambia","Georgia","Germany","Ghana",
            "Greece","Grenada","Guatemala","Guinea","Guinea-Bissau",
            "Guyana","Haiti","Honduras","Hungary","Iceland",
            "India","Indonesia","Iran","Iraq","Ireland",
            "Israel","Italy","Jamaica","Japan","Jordan",
            "Kazakhstan","Kenya","Kiribati","Korea, North","Korea, South",
            "Kosovo","Kuwait","Kyrgyzstan","Laos","Latvia",
            "Lebanon","Lesotho","Liberia","Libya","Liechtenstein",
            "Lithuania","Luxembourg","Macedonia","Madagascar","Malawi",
            "Malaysia","Maldives","Mali","Malta","Marshall Islands",
            "Mauritania","Mauritius","Mexico","Micronesia, Federated States of","Moldova",
            "Monaco","Mongolia","Montenegro","Morocco","Mozambique",
            "Myanmar (Burma)","Namibia","Nauru","Nepal","Netherlands",
            "New Zealand","Nicaragua","Niger","Nigeria","Norway",
            "Oman","Pakistan","Palau","Panama","Papua New Guinea",
            "Paraguay","Peru","Philippines","Poland","Portugal",
            "Qatar","Romania","Russia","Rwanda","Saint Kitts and Nevis",
            "Saint Lucia","Saint Vincent and the Grenadines","Samoa","San Marino","Sao Tome and Principe",
            "Saudi Arabia","Senegal","Serbia","Seychelles","Sierra Leone",
            "Singapore","Slovakia","Slovenia","Solomon Islands","Somalia",
            "South Africa","South Sudan","Spain","Sri Lanka","Sudan",
            "Suriname","Swaziland","Sweden","Switzerland","Syria",
            "Taiwan","Tajikistan","Tanzania","Thailand","Togo",
            "Tonga","Trinidad and Tobago","Tunisia","Turkey","Turkmenistan",
            "Tuvalu","Uganda","Ukraine","United Arab Emirates","United Kingdom",
            "United States of America","Uruguay","Uzbekistan","Vanuatu","Vatican City (Holy See)",
            "Venezuela","Vietnam","Yemen","Zambia","Zimbabwe"
        };

        /// <summary>
        /// List of the United States (full names)
        /// </summary>
        [LibraryBook("US States")]
        public static List<string> USStates = new List<string>
        {
            "Alabama","Alaska","Arizona","Arkansas","California","Colorado","Connecticut","Delaware","Florida",
            "Georgia","Hawaii","Idaho","Illinois","Indiana","Iowa","Kansas","Kentucky","Louisiana","Maine","Maryland",
            "Massachusetts","Michigan","Minnesota","Mississippi","Missouri","Montana","Nebraska","Nevada",
            "New Hampshire","New Jersey","New Mexico","New York","North Carolina","North Dakota","Ohio","Oklahoma",
            "Oregon","Pennsylvania","Rhode Island","South Carolina","South Dakota","Tennessee","Texas","Utah",
            "Vermont","Virginia","Washington","West Virginia","Wisconsin","Wyoming","District Of Columbia",
            "Puerto Rico","Guam","American Samoa","U.S. Virgin Islands","Northern Mariana Islands"
        };

        /// <summary>
        /// List of the United States (official abbreviations)
        /// </summary>
        [LibraryBook("US Abbreviations")]
        public static List<string> USAbbrev = new List<string>
        {
            "AK","AL","AZ","AR","CA","CO","CT","DE","FL","GA",
            "HI","ID","IL","IN","IA","KS","KY","LA","ME","MD",
            "MA","MI","MN","MS","MO","MT","NE","NV","NH","NJ",
            "NM","NY","NC","ND","OH","OK","OR","PA","RI","SC",
            "SD","TN","TX","UT","VT","VA","WA","WV","WI","WY"
        };

        /// <summary>
        /// Names of Santa's reindeer
        /// </summary>
        /// <remarks>Common test account names.</remarks>
        [LibraryBook("Reindeer")]
        public static List<string> Reindeer = new List<string>
        {
            "Dasher", "Dancer", "Prancer", "Vixen", "Comet", "Cupid", "Donner", "Blitzen"
        };

        /// <summary>
        /// Surface the list of encodings supported by dotNet
        /// </summary>
        [LibraryBook("Encodings")]
        public static List<string> Encodings = System.Text.Encoding.GetEncodings().Select(x => x.Name).ToList();
    }
}
