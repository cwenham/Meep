# Meep

(Short for Message Pipeline) 

Use XML to create realtime message processing pipelines that receive, filter, 
modify, act upon, and output discrete messages. Meep is System.Reactive + XML + Plugins.

E.G.: Every 10 seconds ping a host, then--in batches of 5 at a time--store the
responses in a table called "Pings" in a database called "Example".

    <Batch MaxSize="5" s:Store="Example:Pings">
        <Ping To="www.mit.edu">
            <Timer Interval="00:00:10"/>
        </Ping>
    </Batch>
    
If you don't bother to specify the database, Meep will default to creating a 
SQLite database and a table based on the output of &lt;Ping&gt;, which is 
already marked up to use appropriate SQL data types, indexes and keys.

## Command-Line, .Net-Standard Library, And Plugins

Meep comes as a standalone command-line app, and as a .Net-Standard library that
you can use in your own projects. It supports plugins, so it's very easy to
create your own pipeline verbs. Here's one:

    public class Reverse : AMessageModule
    {
        public override async Task<Message> HandleMessage(Message msg)
        {
            string reversedText = new string(msg.ToString().Reverse().ToArray());

            return new StringMessage
            {
                DerivedFrom = msg,
                Value = reversedText
            };
        }
    }
    
This is how you'd use it:

    <Plugin Path="/path/to/my/plugin/Reverse.dll"/>
    
    <WriteLine From="{msg.Value}">
        <Reverse>
            <Get Url="http://doineedajacket.com">
                <Timer Interval="3:00:00"/>
            </Get>
        </Reverse>
    </WriteLine>
    
## SmartFormatted Arguments
    
Meep uses [SmartFormat](https://github.com/axuno/SmartFormat.NET) for almost
every module parameter, so you can modify the above to look like this:

    <WriteLine From="{fam.Timer.Payload}: {msg.Value}">
        <Reverse>
            <Get Url="{msg.Value}">
                <Timer Interval="3:00:00" Payload="http://doineedajacket.com">
                <Timer Interval="1.0:00:00" Payload="http://isabevigodadead.com">
            </Get>
        </Reverse>
    </WriteLine>
    
Timer intervals are .Net TimeSpans, so 3:00:00 is 3 hours, and 1.0:00:00 is
one day.
    
**msg** is the root of the current message. **fam** is used to address the 
output of modules further upstream (and you can `Name` them properly, when
"Timer" is no longer enough). There's also **cfg** to get to configuration and 
**rgx** to address the groups and captures of a regular expression match
returned by the &lt;Match&gt; filter.

## Complex pipelines
            
The messages coming from the two Timers get merged into a single stream before 
they're passed into Get. You can have as many upstreams as your hardware and 
performance requirements can support. 

You can also `Tap` into any point of a sister pipeline like this:

    <WriteLine Name="Claims" From="Claim filed at {msg.URL}">
        <Listen Base="http://local.domain.com/Spam" />
        <Listen Base="http://local.domain.com/Copyright" />
    </WriteLine>

    <BayesTrain Class="{msg.URL}">
        <Tap From="Claims"/>
    </BayesTrain>
    
    <Respond>
        <Text>Thank you for your claim, it will be processed in the next 4-5
        business days.</Text>

        <Tap From="Claims"/>
    </Respond>
    
## Built-in Web Server

Meep implements a simple web server based on .Net's built-in `HttpListener`, but
every message fed into the pipeline by &lt;Listen&gt; contains a live 
`HttpListenerContext`, and the &lt;Respond&gt; module can reply to it.

## Macros and Shorthand to tame the XML

XML was chosen because it supports namespaces and comments, and there is a large
ecosystem of tools for editing and working with XML. But XML has a heavy syntax,
and Meep has its own XML deserialiser to provide some convenient syntax sugar
to help in some spots.

Meep uses NLog, but the WriteLine module is also useful for debugging and it
can use a shorthand XML syntax to avoid cluttering our pipeline definitions:

    <Reverse WriteLine="{fam.Timer.Payload}: {msg.Value}">
        <Get Url="{msg.Value}">
            <Timer Interval="3:00:00" Payload="http://doineedajacket.com">
            <Timer Interval="1.0:00:00" Payload="http://isabevigodadead.com">
        </Get>
    </Reverse>
    
You can have your own modules support the optional shorthand syntax with a code
attribute, like this:

    [Macro(Position = MacroPosition.Downstream)]
    public class Reverse : AMessageModule
    {
       ...
    
Now you could write this:

    <Get Url="{msg.Value}" Reverse="" WriteLine="{fam.Timer.Payload}: {msg.Value}">
        <Timer Interval="3:00:00" Payload="http://doineedajacket.com">
        <Timer Interval="1.0:00:00" Payload="http://isabevigodadead.com">
    </Get>
    
When reading a pipeline in XML, **left is downstream and right is upstream**.
Imagine looking at a water fountain sideways; the Timers are the fountainheads
and their messages flow down through as many dishes as you want before emptying
into the gutter. The dishes are modules that filter, modify or act on the 
messages as they flow down (left).

## Or no XML

The XML is used as an Object Instantiation Language, but you can use Meep without
it. This is how Meep loads its own pipelines:

    public Bootstrapper(string filename)
    {
        FileChanges changes = new FileChanges
        {
            Path = Path.GetDirectoryName(filename),
            Filter = Path.GetFileName(filename)
        };

        Load load = new Load
        {
            From = "{msg.FullPath}"
        };
        load.AddUpstream(changes);

        DeserialisePipeline deserialiser = new DeserialisePipeline();
        deserialiser.AddUpstream(load);

        _laces = deserialiser;
    }
    
`FileChanges`, `Load` and `DeserialisePipeline` are all pipeline modules. You 
can connect them together with `.AddUpstream()`.
    
In the above, Meep is using its own modules for loading and deserialising 
pipelines and automatically reloading them when the file changes.

The Boostrapper class also has constructors for reading pipelines from a URL
or Git repository, and set themselves up with a Timer to reload changes. Both of
those are also hard-coded pipelines.

You can move between XML and hard-coded pipelines for experiment, prototyping, 
unit testing and debugging with tools like LinqPad, then deploy to production in 
either form.

## Namespaces

Meep uses reflection to discover all the modules in your plugin's DLL, so the
name you use in the XML is the same as the class name in code.
    
We recommend that you think very carefully about the name of your module so it
makes sense and won't conflict with others, but we recommend using namespaces 
even more. To put your module in a custom namespace, just use a code attribute 
like this:

    [MeepNamespace("http://meep.example.com/ExamplePlugin/V1")]
    public class Reverse : AMessageModule
    {
        ...
        
Then you'd declare it in the XML header and use it like this:

    <?xml version="1.0" encoding="UTF-8"?>
    <Pipeline xmlns="http://meep.example.com/Meep/V1"
              xmlns:ep="http://meep.example.com/ExamplePlugin/V1">
              
        <ep:Reverse>
            ...
            
                  
You can use more than one namespace per plugin, or no namespace at all if you're
careful.       
               
## Built-In Modules

Timers are just one kind of fountainhead, or _source_. Here are some others
built into Meep:

   * **Emit** Emit individual paragraphs (or words) from a common text such as Lorem Ipsum or a list of countries
   * **Fibonacci** Produces the next number in a Fibonacci sequence
   * **FileChanges** Generates a message every time a file is created or changes on disk
   * **Get** Fetches the contents of a URL
   * **Listen** for incoming HTTP requests. Combined with **Respond** to implement a basic HTTP server
   * **Load** Loads a file from disk
   * **Localise** Caches a resource at a remote URL, either to disk or memory
   * **Ping** Pings a remote host
   * **Random** Generates a random number
   * **SubPipeline** Runs another pipeline in a separate process
   * **TcpClient** Listens for data coming from a TCP socket
   
Most of these modules are designed to be paired with Timer or another module
that can trigger its action, but they are interchangable. In the examples above 
we've used Timer to trigger Get, but you could also use FileChanges to trigger a 
Get, or Load the file that changed, and so-on.

### Built-In Filters

   * **Bayes** categorisation and training, with a native implementation
   * **Bloom** for managing cache hits
   * **Distinct** and **DistinctUntilChanged** for ignoring dupes
   * **Match** RegEx matching
   * **Skip** Skip n-many messages
   * **Where** using [NCalc](https://github.com/sklose/NCalc2) expressions
   
The output message of **Pattern** includes the Match object, which you can
inspect downstream in {Smart.Format} expressions.
   
### Built-In Outputs and Modifiers

   * **Delete** HTTP
   * **Email**
   * **Extract** subsets according to XPath, JPath or RegEx
   * **Post** HTTP
   * **Put** HTTP
   * **Recombine** genomes defined in XML, for making genetic algorithms
   * **Respond** to inbound HTTP requests taken by **Listen**
   * **Save** to file
   * **Split** delimited data into columns and rows
   * **Unzip** files
   * **WriteLine** to console 
           
## Meep's Bundled Plugins
            
MeepLib only supports a few modules on its own so its base NuGet package can 
remain a pure .Net-Standard library with minimal dependencies. So the host Meep 
program bundles it with three plugins to make it more useful out-of-the-box: 
MeepGit, MeepSQL, and MeepSSH. 

MeepGit supports Git operations like Clone, Pull, Fetch, and Checkout. In
addition to exposing these functions as modules you can use, Meep uses them
itself so it can load pipelines from any repository. If you've distributed the 
work of a resource-intensive pipeline across many machines it's easy to update 
them all with a single Push.

MeepSQL connects to databases and does what it says on the tin: all CRUD
operations, arbitrary SQL queries, plus some convenience functions. You can use 
any database supported by .Net ADO, and it includes default support for SQLite: 
if you need to "just dump it somewhere" then remember our first example at the 
top of this page: all the defaults are chosen for zero-config usage.
        
MeepSSH can connect to any SSH host with password or private key authentication,
converting upstream messages into commands executed on the remote host, and the
output of those commands into messages passed downstream.

Meep is based on System.Reactive and handles messages asynchronously and in 
realtime.

**Warning:** Meep is still in early development, so there will be breaking 
changes coming.
