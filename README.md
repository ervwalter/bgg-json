bgg-json
========

This is a sample wrapper for the [BoardGameGeek JSON API](http://boardgamegeek.com/wiki/page/BGG_XML_API2).  It is developed in C# and uses the ASP.NET Web API to reexpose selected BGG API endpoints as JSON endpoints.

Note, this code is not intended to be comprehensive and lacks things that a production solution would need (e.g. throttling and caching).

This sample code is running live at [bgg-json.azurewebsites.net](http://bgg-json.azurewebsites.net/). You are welcome to use that server if you like for some test code, but be aware that it is hosted on the free tier of Azure and has extremely low quotas for how much CPU and RAM it can use before Azure temporarily shuts it down for an hour.  So it's not viable for you to use it for production purposes unless you don't care about reliability.

See this forum post for more information and for any questions: <http://boardgamegeek.com/thread/1109812/jsonjsonp-wrapper>.

License
========

The MIT License (MIT)

Copyright (c) 2014 Erv Walter

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
