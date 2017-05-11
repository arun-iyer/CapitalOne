2There are 3 projects in the CapitalOne solution.

-----------------------------------

Project #1: CapitalOne.

This project build to a console application. CapitalOne.exe
The code in this project makes calls into the library LM_Operations (LM = Level Money)

-----------------------------------

Project #2: LM_Operations

This project builds to a DLL: LM_Operations.dll
All the necessary operations to support the requirements for loading the user's transactions, figuring out monthly debit/credit, ouptput the numbers in the specified format, ignoring donuts and credit card payments.

-----------------------------------

Project #3: LM_Tests

This is a Unit test project with gets around 96.6% code coverage.

-----------------------------------

Binaries: The binaries folder has CapitalOne.exe, LMOperations.dll and Newtonsoft.Json.dll

--> LMOperations.dll

--> Newtonsoft.Json.dll is an opensource json serializer/de-serializer.

--> CapitalOne.exe runs in console mode, supports two command line option:
-ignore-donuts
-ignore-cc-payments

-------------------------------------

If you need to open and build the projects, look for the solution file named CapitalOne.sln. Opening this file in Visual Studio will load all the 3 projects mentioed above. You can then build it and run it from Visual Studio. Unit tests can be run inside visual Studio, all the unit tests should pass.

Send me an email at aruniy@hotmail.com if you have any questions.

------------------------------------
