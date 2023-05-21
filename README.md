There are very mature libraries out there for background job processing such as [Hangfire](https://www.hangfire.io/) and [Quartz.Net](https://www.quartz-scheduler.net/). My feeling at the moment, though, is that these libraries aren't as simple to use as I'd like. Also, I'm eager to make the most of .NET's built-in [BackgroundService](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-7.0&tabs=visual-studio#backgroundservice-base-class) class, which is the springboard for this project. I've run into some gotchas with `BackgroundService` (namely unexpected app stoppage in IIS causing background jobs to stop unexpectedly), but I really like that `BackgroundService` is a .NET native feature.

For completeness' sake I should mention that you can also use Azure functions to add background job processing to your applications. (Not to mention other "serverless" cloud capability like AWS Lambda.) But the truth is I have not enjoyed working with Azure functions. I find them hard to debug and monitor, and they require a large context shift away from the rest of your application.

So, this library extends the [BackgroundService](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-7.0&tabs=visual-studio#backgroundservice-base-class) class with:

- [CronJobBackgroundService](https://github.com/adamfoneil/BackgroundService.Extensions/blob/master/BackgroundService.Extensions/CronJobBackgroundService.cs), based on [sgbj/crontimer](https://github.com/sgbj/crontimer).
- [SqlServerQueueBackgroundService](https://github.com/adamfoneil/BackgroundService.Extensions/blob/master/BackgroundService.Extensions/SqlServerQueueBackgroundService.cs), which had critical help from this blog [Using tables as queues](http://rusanu.com/2010/03/26/using-tables-as-queues/).

At the moment there's no NuGet package.
