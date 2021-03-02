# Peep

### TL:DR
This is a distributed .NET Core crawler, which utilises [PuppeteerSharp](https://github.com/hardkoded/puppeteer-sharp) to extract data from websites. I can deploy it in Docker containers across any number of devices and can interact with it via a custom web API which receives crawl jobs, publishes them via RabbitMQ to running crawler nodes and then aggregates the results.

My aim is to make use of Kubernetes as well, to orchestrate how many nodes are running at once, this could then be used to scale up/down the crawler based on how many jobs are queued and waiting to be run!

### Yet another web crawler

Throughout my developer career I think I've made at least 10 different web crawlers. The idea with all of them was to create an interesting application that could be used by me to practice some advanced concepts, such as how to make a multi-threaded application, or how to make my code as efficient as possible, all while providing a tool that could actually be useful.

With most of them though, I would end up sacrificing the usefulness part of it and get hyper focussed on how fast it could be, I'd set a target of X many crawled pages per minute, and then slam as many threads on as possible and just completely abuse a large website like reddit until I lost interest. I paid little regard to how easy it was to use the crawler, or how nice it was to websites, or if it could actually extract any data at all.

It wasn't until maybe the 8th version that I even realised there was a fatal flaw with my crawler. **It was terrible at extracting data from websites that relied on JavaScript to render**. You see, at the heart of any crawler is the part that actually makes requests to websites, of course it is, in my crawler this is done by the `HttpWebRequest` class. It's a really simple API that allows you to make a request and then read the response as a stream. This way works perfectly with sites that actually give you all the HTML they're going to use to render the website, but this completely ignores all the content that's added dynamically, by JavaScript.

This was a big problem, if I wanted to extract data from a website that used, say, React to manipulate its content, my crawler wouldn't be able to see any of it and just get a very basic version of the site's HTML, perhaps with a few placeholder divs and some script/style includes. I needed to get rid of the `HttpWebRequest` and use something more sophisticated.

### PuppeteerSharp
Something more sophisticated came along in the form of [PuppeteerSharp](https://github.com/hardkoded/puppeteer-sharp), a really good .net library that allows you to control a headless chrome browser. This is exactly what I needed, now I can access websites, wait for them to properly render and then extract any bit of data I may want and all with a modern, task based API.

I got to work to update the crawler to use PuppeteerSharp. I had a great deal of trouble getting it to work in a multi-threaded way. Whenever I tried to crawl a site PuppeteerSharp would deadlock when navigating to a page. After many hours of tinkering and googling, I decided it would just be easier to have it single-threaded, and just take the loss of potential Crawls Per Minute on the chin.

### Distributed
I spent about 5 minutes being content with the crawler, before deciding I should change it to be distributed, where multiple nodes, each with their own puppeteersharp instance, could work together on a crawl job. To manage the operation, I created a .net core web api, which would receive crawl jobs from me and then publish them out to the crawlers. 

I went through a few different iterations of this, where the API would drop a json file of the job in a folder that each crawler would monitor, which worked okay, but felt a bit analogue. Instead, I thought it best to make use of some kind of messaging and message broker. 

After a quick google I found RabbitMQ, and got to work in updating the API and crawlers to publish and consume messages through it. It wasn't long before I was back on google, as I found the .net client for RabbitMQ to be quite basic and old-school. There were a lot more string parameters and void methods than I was comfortable with. After a while of working in the Type-safe, generic heavy world of .net core, this felt a bit... archane.

The [MassTransit](https://masstransit-project.com/) nuget package ended up as part of my projects, as it hid away all of the implementation details of RabbitMQ that I didn't like and provided what I think is a really nice to use API.

I then needed a place to store things that the crawlers would need to share, such as a URL queue, a filter that contains already crawled pages and a place the crawlers can deposit the data they've found. For this, I added a Redis cache in it's own docker container.

Finally, I grabbed a load of raspberry pis that I had lying around, and combined them all to make my distributed crawler. Even though all the different parts of my crawler have their own docker containers, and therefore can be run anywhere, such as AWS, I thought it would be nice to have an actual physical thing I can point to and say _that's a crawler, that_. 

### Next steps
There are a couple of issues I need to address before saying this project is complete. 

1. I don't think the redis stores for queue, filter and even data are 100% thread-safe, I will sometimes see copies of the same bit of data from a website in the output of a crawl, which I can only assume means that multiple crawlers are crawling the same pages. It looks like my code to interact with Redis just needs some more locks in them
2. The web api is a bit fire-and-forget - it uses RabbmitMQ to publish crawl jobs to the crawlers, but doesn't wait for any of them to respond, it waits for the message broker to respond, but I think it may be better if I could get the crawlers to return a response saying _Yep I'm crawling that for you_. This would be beneficial later on when the API is deciding to stop the crawl (beit from outside cancellation, or one of the stop conditions has been reached). At the moment, It doesn't actually know which crawlers, if any, are working on the crawl job, or if they ever even started, or had to stop partway through due to an error. A mechanism to somehow "recruit" crawlers for a job would be better in my opinion.
3. The crawler has no way to respond to high traffic. For this point I'd really like to use Kubernetes, it'd be great if the system could notice a lot of jobs waiting in the job queue, and then respond by bringing up more instances of the crawlers and then push different jobs onto those new crawlers. It should then be able to reduce the amount of crawlers working in quiet periods. This would take some work, but would be a great opportunity to learn about this kind of thing!
