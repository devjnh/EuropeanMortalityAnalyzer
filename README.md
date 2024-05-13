# EuropeanMortalityAnalyzer
This program allows to generate statistics with European mortality statistics published by EuroStat.
## How to use the executable
You can download the already built executable for Windows with the following link:  
[Download EuropeanMortality.zip](https://github.com/devjnh/EuropeanMortalityAnalyzer/releases/latest/download/EuropeanMortality.zip)  
Extract all files in the zip file in a new folder. You can then launch the the executable EuropeanMortalityAnalyzer.exe.
The first execution will automatically:

- Create in the working folder a subfolder named *Data* were all the files will be stored
- Download the mortality statistics from EuroStat
- Download the age structure statistics also from EuroStat
- Download the Covid vaccination statistics from the ECDC web site. 
- Insert all the statistics in a SQLite database named *EuropeanMortality.db*
- Calculate weekly death statistics by 5 years age interval standardized according to the age structure

The first execution will take some time to download and insert all the data in the database and then build the death statistics. So, you need to be patient.

Then at every execution, the program will generate some MS Excel spreadsheet depending on the command line.
By default the following files are generated:  
- *Europe - West.xlsx* Mortality of countries of west europa
- *Europe - West 10-40.xlsx* Mortality of countries of west Europa for the age of 10 years to 40 years excluded.

Here is what the example batch file looks like :

    .EuropeanMortalityAnalyzer.exe countries -–country FR ES IT
    .EuropeanMortalityAnalyzer.exe countries -–country FR ES IT --MinAge 5 --MaxAge 40 
    .EuropeanMortalityAnalyzer.exe area -–area FR ES IT –area “France Italy Spain” --MinAge 5 --MaxAge 40
The batch file will generate the following files:
France.xls Mortality of France
  
- *Spain.xls* Mortality of Spain
- *Italy.xls* Mortality of Italy
- *France 5-40.xls* Mortality of France for ages from 5 to 40 years
- *Spain 5-40.xls* Mortality of Spain for ages from 5 to 40 years
- *Italy 5-40.xls* Mortality of Italy for ages from 5 to 40 years
- *France Spain Italy 5-40.xls* Mortality of France, Spain and Italy together for ages from 5 to 40 years 

You can run your own examples by changing various parameters. For more information on what you can specify as arguments launch:

    EuropeanMortalityAnalyzer.exe help

## How to build the executable
If you want to build the executable by yourself and review or change the code, you need to download the code from this repository and you need to download and install Visual Studio 2022. You can use the free [community edition](https://visualstudio.microsoft.com/vs/community/).  
With Visual Studio open the solution *EuropeanMortalityAnalyzer.sln* and build it.
The executable for Windows should be generated in the *EuropeanMortalityAnalyzer\bin\Debug\net72* subfolder.
## How to distribute the executable
For Windows computers copy the executable *EuropeanMortalityAnalyzer.exe* along with all the dll files from the *EuropeanMortalityAnalyzer\bin\Debug\net72* folder.  
For Linux or MacOSX you can try the .NET 6 version of the executable generated in the folder *EuropeanMortalityAnalyzer\bin\Release\net6.0*. But this was not tested yet.

