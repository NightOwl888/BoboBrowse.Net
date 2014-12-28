This is a port of the original CarDemo application in Java to MVC 4. It
demonstrates how to configure the BoboBrowse.Net framework to do a faceted
(drill-down) search.

JavaScript

Faceted search is a cumulative process from a UI perspective. Facets are 
typically selected one after another and the expected result is that all 
of them will be applied to the output. Therefore, when hosting in a web 
application, it typically works best to use AJAX to supply the facet data 
and update the results list so the selected facet data is not lost.

Since this is a port from an older demo, it uses quite a bit of JavaScript 
code rather than taking full advantage of the JQuery framework. In a modern 
MVC application, you would typically use JQuery instead to create selection
controls and update the feedback on the UI.

The JavaScript used in this demo is in the "/Scripts/bobo" directory.

Controllers and Models

This demo uses a single HomeController.Browse() action method to do most of
the heavy lifting. 

There are several view models in the Models directory 
that are essentially just abstraction wrappers around the BrowseRequest 
and BrowseResult types. Using wrappers in this way ensures that the JavaScript
code doesn't need to change if the BoboBrowse.Net API changes in the future.

BoboServices

The BoboDefaultQueryBuilder and BrowseRequestConverter types are to 
make the conversion between BoboRequest (the view model) and BrowseRequest.

The BrowseService is to abstract away the facet configuration code 
from the controller.

Spring.Net XML Configuration

The BrowseService contains an example of how the facet handlers could be 
configured in code. However, in this demo the facet handlers are 
configured in the Spring.Net XML configuration file named 
"/LuceneIndex/bobo.spring". The configuration file is automatically 
used under the following conditions:

1. The NuGet package BoboBrowse.Net.Spring is installed into the project.
2. There is a Spring.Net XML configuration file named bobo.spring in the
same directory as the Lucene.Net index.
3. No facet handlers were passed into the BoboIndexReader.CreateInstance() 
method (the facetHandlers argument is null).
