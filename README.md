# Meep

(Short for Message Pipeline) 

Use XML to create realtime message processing pipelines that receive, filter, 
modify, act upon, and output discrete messages.

E.G.: Every 10 seconds ping a host, then--in batches of 5 at a time--store the
responses in a table called "Pings" in a database called "Example".

    <Batch MaxSize="5" s:Store="Example:Pings">
        <Ping To="www.mit.edu">
            <Timer Interval="00:00:10"/>
        </Ping>
    </Batch>
    
If you don't bother to specify the database, Meep will default to creating a 
SQLite database and a table based on the output of &lt;Ping&gt;, which is already
marked up to use appropriate SQL data types, indexes and keys.

Meep comes as a standalone command-line app, and as a .Net-Core library that
you can use in your own projects. It supports plugins, so it's very easy to
create your own pipeline verbs.

It's based on System.Reactive and handles messages asynchronously and in 
realtime.

**Warning:** Meep is still in early development, so there will be breaking 
changes coming.