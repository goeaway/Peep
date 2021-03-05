# How to update the DB

[reference](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)

We want to make sure our code models and the DB tables stay in sync. When adding new properties and objects we must update the database through migrations...

### Setup
* Make sure the migrations assembly has the `Microsoft.EntityFrameworkCore.Tools` nuget package
* Make sure the migrations assembly has a reference to where the DbContext is
* Make sure the `Peep.API` references the migrations Assembly
* Make sure the `Peep.API` has the `Microsoft.EntityFrameworkCore.Design` package  
* Make sure the `Peep.API` specifies the migrations assembly in the DbContext options builder
```
options.UseSqlServer(
    Configuration.GetConnectionString("DefaultConnection"), 
    x => x.MigrationsAssembly("Peep.API.Persistence")));
```
* Make sure the `Peep.API.Program` Main method looks like the below so the migrations are run if required on startup
```
public static void Main(string[] args)
{
    var host = CreateHostBuilder(args).Build();

    var context = host.Services.GetRequiredService<PeepApiContext>();
    context.Database.Migrate();
        
    host.Run();
}
```

### Updating
* Navigate to the directory where the EF context type is held
* In the terminal, run the following command
```
dotnet ef migrations add MigrationName -s ..\Peep.API
```
* Running that command should result in a couple of files being added to the Migrations folder


### Create SQL script
The below script can be run against a database to perform the migrations on it

`dotnet ef migrations script -s ..\Peep.API`