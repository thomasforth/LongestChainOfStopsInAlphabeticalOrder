# LongestChainOfStopsInAlphabeticalOrder

A .NET 2 console application to Find the longest chain of consecutive public transport stop that are also in alphabetical order in Great Britain. It relies on the Traveline National Dataset (TNDS), which is available from (Traveline Open Data)[http://www.travelinedata.org.uk/].

# How to run it?
The Easiest way to run this program is to open the solution in Visual Studio 2017, change `string basePath = @"D:\Dec_2017_traveline\";` in `Program.cs` to point to unzipped TransXChange format xml files you've downloaded from Traveline, and run the program. .NET 2 applications can run on Linux, and Mac OS too if you like, you'll need to download the .NET 2 runtime. Expect it to take about 10 minutes on a 3GHz Intel Core i7.

# Just show me the results
There's both an Excel file (.xlsx) and a .csv file of the results in the `LongestChainOfStopsInAlphabeticalOrder` folder. They are the results that were generated by the program run on 2018-06-02.

# What are the highlights?
* There are 16715 unique services and 380552 unique journey patterns in the Traveline National Dataset. (a single service can run many patterns).
* There is a 12 stop bus sequence in Newcastle, but the stops are numbered which i don't like. They are Barrack Road N/b, Bbc Tv Centre N (08926), Fenham Hall Drive W (08223), Fenham Hall Drive W (08241), Fenham Hall Drive W (08242), Fenham Hall Drive W (08243), Fenham Hall Drive W (08244), Fenham W (08245), Netherby Drive Nw (08246), Netherby Drive Nw (08247), Stamfordham Road W (08089), Stamfordham Road W (08090).
* I consider the best of the longest consecutive alphabetical bus journeys in Great Britain to be in Tamworth. 11 stops, Bus Service 7, Tamworth - Stonydelph, which stops at the following named stops in order, Ealingham, Fossdale, Gayle, Grindsbook, Hawkside, Hebden, Lintly, Lowforce, Mellwaters, Mossdale, Murton.
* The onther 11 stop sequence is on service 205, Dewsbury - Pudsey, which stops at the following named stops in order, Commonside Black Labrador PH, Commonside Bromley Street, Commonside Wood Lane, High Street Commonside, High Street Highgate St, High Street Kirkgate, High Street Rathlin Rd, John Ormsby Way Leeds Rd, Owl Lane Dewsbury Rams Stadium, Windsor Rd Owl Lane, Windsor Rd Windsor Close.
* The longest consecutive alphabetical tram journey in Great Britain is in Manchester. 6 stops, Etihad Campus, Holt Town, New Islington, Piccadilly, Piccadilly Gardens, St Peter's Square.
* The longest consecutive tube journey is 5 stops long. There are multiple examples, on the Central, Circle, District, Hammersmith & City, Jubilee, Metropolitan, Northern, and Picaddilly lines. Start at Leyton, Baker Street, Barons Court, Bermondsey, Croxley, Harrow-on-the-Hill, Borough, Arsenal, or Eastcote. There is a single example of 6 stops long that was timetabled only for a very brief period when London Bridge station was closed on the Southbound Northern Line. The sequence was Bank, Borough, Elephant & Castle, Kennington, Oval, Stockwell.
* The longest consecutive coach jouney is 6 stops long. The 303 Rothwell - Market Harborough - Melton Mowbray, with consecutive stops at Bus Station, Chapel Building, Church Lane Hail & Ride, Horseshoe House Hail & Ride, Lowesby Lane Hail & Ride, Petrol Station Hail & Ride, The Farm Hail & Ride.
* Two ferry journeys have 5 consecutive stops that are in alphabetical order. Both in London they are, Bankside Pier, Blackfriars Pier, Embankment Pier, Millbank Pier, Vauxhall St. George Wharf Pier and Battersea Power Station Pier, Cadogan Pier, Chelsea Harbour Pier, PLANTATION WHARF PIER, Wandsworth Riverside Quarter Pier.

# Are you sure your calculations are right?
No.

# What about trains?
The Traveline National Dataset provides timetables for Ferries, Coaches, Buses, Underground, Metro, and Light Rail in the TransXChange format. National Rail (train) timetables are available in another format (CIF) from a different website -- (ATOC data)[http://data.atoc.org/]. I may extend my analysis to this dataset in the future. I don't have time now.

# License
MIT license.
