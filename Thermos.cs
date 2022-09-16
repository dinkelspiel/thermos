using System.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;



namespace thermos;

//Response(json.dumps("Provided path is not a directory"), status=400, mimetype='application/json')

public class Response {
    public string responseValue = "";
    public int statusCode = 200;
    public string mimetype = "text/html";

    internal Response() {}

    public Response(string response, int status=200, string mimetype="text/html") {
        this.responseValue = response;
        this.statusCode = status;
        this.mimetype = mimetype;
    }
}

public class Route : Attribute {
    public string route = "";
    public string[]? methods = null;

    public Route(string route, string[]? methods=null) {
        this.route = route;
        this.methods = methods;
    }
}

struct RouteInfo {
    public MethodInfo method;
}

struct RouteContainer {
    public Dictionary<string, RouteInfo> methods;
    public RouteContainer() {
        methods = new Dictionary<string, RouteInfo>();        
    }
}

public class Thermos {
    static HttpListener listener;
    static string url;
    static Dictionary<string, RouteContainer> routes = new Dictionary<string, RouteContainer>();

    public static async Task HandleIncomingConnections() {
        bool runServer = true;

        // While a user hasn't visited the `shutdown` url, keep on handling requests
        while (runServer)
        {
            // Will wait here until we hear from a connection
            HttpListenerContext ctx = await listener.GetContextAsync();

            // Peel out the requests and response objects
            HttpListenerRequest req = ctx.Request;
            HttpListenerResponse resp = ctx.Response;

            // Print out some info about the request
            // Console.WriteLine(req.Url.ToString());
            // Console.WriteLine(req.HttpMethod);
            // Console.WriteLine(req.UserHostName);
            // Console.WriteLine(req.UserAgent);
            
            // Console.WriteLine(DateTime.Now);

            // Console.WriteLine(req.Url.AbsolutePath);

            var _argumentIndex = 0;
            List<int> _routeCandidates = new List<int>();
            List<int> _routeReplaces = new List<int>();
        
            for(var i = 0; i < routes.Keys.Count; i++) {
                _routeCandidates.Add(i);
            }

            foreach(string _argument in req.Url.AbsolutePath.Split("/")) {
                var argument = _argument == "" ? "/" : _argument; 
                var userPath = req.Url.AbsolutePath;

                // Console.WriteLine("Absolute Path: " + req.Url.AbsolutePath);

                var _routeIndex = 0;
                foreach(string checkRoute in routes.Keys) {
                    if(!checkRoute.Contains("<") && checkRoute != userPath) {
                        _routeCandidates.Remove(_routeIndex);
                    }

                    if(checkRoute.Split("/").Length != userPath.Split("/").Length) {
                        _routeCandidates.Remove(_routeIndex);
                    }

                    _routeIndex ++;
                }
                _argumentIndex ++;
            }

            // Console.WriteLine($"Route Candidates {req.Url.AbsolutePath}");
            // foreach(var i in _routeCandidates) {
            //     Console.WriteLine(" " + routes.Keys.ToArray()[i]);
            // }

            // Console.WriteLine(_routeCandidates.Count);

            // if(_routeCandidates.Count > 1) {
            //     throw new ConflictingRoutesException(req.Url.AbsolutePath);
            // }

            var selectedRoute = "";
            List<string> _finalRoute = new List<string>();

            List<dynamic> _arguments = null;
            List<dynamic> _argumentsTypes = new List<dynamic>();
            if(_routeCandidates.Count != 0) {
                var routeCandidate = _routeCandidates[0];
                selectedRoute = routes.Keys.ToArray()[routeCandidate];
                
                for(var i = 0; i < req.Url.AbsolutePath.Split("/").Length; i++) {
                    if(selectedRoute.Split("/").Length == 0 || req.Url.AbsolutePath.Split("/")[i].Length == 0) {
                        _finalRoute.Add(selectedRoute.Split("/")[i]);
                        continue;
                    } 
                    if(selectedRoute.Split("/")[i][0] == '<' && selectedRoute.Split("/")[i].Last() == '>') {
                        if(_arguments == null)  
                            _arguments = new List<dynamic>();

                        if(selectedRoute.Split("/")[i].Split(":").Length == 1) {
                            _argumentsTypes.Add("string");
                        } else {
                            switch(selectedRoute.Split("/")[i].Split(":")[0].ToLower().Substring(1, selectedRoute.Split("/")[i].Split(":")[0].ToLower().Length - 1)) {
                                case "string":
                                    _argumentsTypes.Add("string");
                                    break;
                                case "int":
                                    _argumentsTypes.Add("int");
                                    break;
                                case "float":
                                    _argumentsTypes.Add("float");
                                    break;
                                case "path":
                                    _argumentsTypes.Add("path");
                                    break;
                            }
                        }

                        _finalRoute.Add(req.Url.AbsolutePath.Split("/")[i]);
                        _arguments.Add(req.Url.AbsolutePath.Split("/")[i]);
                        continue;
                    }
                    _finalRoute.Add(selectedRoute.Split("/")[i]);
                }
            }

            // Console.WriteLine(selectedRoute);
            var finalRoute = String.Join("/", _finalRoute);
            // Console.WriteLine(finalRoute);

            bool invalidType = false;

            dynamic arguments;
            if(_arguments != null) {
                arguments = _arguments.ToArray();
                for(var i = 0; i < arguments.Length; i++) {
                    if(_argumentsTypes[i] == "string") {
                        arguments[i] = arguments[i].ToString();
                    } else if(_argumentsTypes[i] == "int") {
                        try {
                            arguments[i] = Int32.Parse(arguments[i]);
                        } catch(Exception e) {
                            invalidType = true;
                        }   
                    } else if(_argumentsTypes[i] == "float") {
                        try {
                            arguments[i] = float.Parse(arguments[i]);
                        } catch(Exception e) {
                            invalidType = true;
                        }   
                    } else if(_argumentsTypes[i] == "path") {
                        arguments[i] = arguments[i].ToString();
                        // TODO: FIX PATH
                    }
                }
            } else
                arguments = null;

            Response response = new Response();

            // Console.WriteLine(selectedRoute);
            // Console.WriteLine(req.Url.AbsolutePath);
            if(invalidType) {
                response.responseValue = Templates.InvalidRoute;
                response.statusCode = 400;
            } else if(!selectedRoute.Contains("<") && selectedRoute != req.Url.AbsolutePath) {
                response.responseValue = Templates.InvalidRoute;
                response.statusCode = 400;
            } else if(!routes.ContainsKey(selectedRoute)) {
                response.responseValue = Templates.InvalidRoute;
                response.statusCode = 400;
            } else if(!routes[selectedRoute].methods.ContainsKey(req.HttpMethod)) {
                response.responseValue = Templates.InvalidMethod;
                response.statusCode = 400;
            } else {
                if(routes[selectedRoute].methods[req.HttpMethod].method.ReturnType == typeof(String))
                    response.responseValue = (string)routes[selectedRoute].methods[req.HttpMethod].method.Invoke(null, arguments);
                else {
                    response = (Response)routes[selectedRoute].methods[req.HttpMethod].method.Invoke(null, arguments);
                }
            }

            string clientIP = ctx.Request.RemoteEndPoint.Address.MapToIPv4().ToString();
            Console.WriteLine(String.Format("{0} - - [{1}] \"{2} {3}\" {4} -", clientIP, DateTime.Now.ToString(), req.HttpMethod, req.Url.AbsolutePath, resp.StatusCode));


            byte[] data = Encoding.UTF8.GetBytes(response.responseValue);
            resp.ContentType = response.mimetype;
            resp.ContentEncoding = Encoding.UTF8;
            resp.StatusCode = response.statusCode;
            resp.ContentLength64 = data.LongLength;

            // Write out to the response stream (asynchronously), then close it
            await resp.OutputStream.WriteAsync(data, 0, data.Length);
            resp.Close();
        }
    }

    public void Run(string host="127.0.0.1", int port=8000) {
        listener = new HttpListener();
        url = String.Format("http://{0}:{1}/", host, port);
        listener.Prefixes.Add(url);
        listener.Start();
        Console.WriteLine("Listening for connections on {0}", url);

    
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach(var assembly in assemblies) {
            var methods = assembly.GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes(typeof(Route), false).Length > 0)
                .ToArray();

            foreach(var method in methods) {
                Route attr = (Route)method.GetCustomAttributes(typeof(Route), true)[0];

                // Console.WriteLine();
                // Console.WriteLine(attr);
                // Console.WriteLine(attr.route);
                if(!(method.ReturnType == typeof(String) || method.ReturnType == typeof(Response))) {
                    throw new InvalidResponseTypeException(attr.route, method.ReturnType.ToString());
                }

                if(!routes.ContainsKey(attr.route))
                    routes.Add(attr.route, new RouteContainer());

                if(attr.methods == null) {
                    routes[attr.route].methods.Add("GET", new RouteInfo() {
                        method = method
                    });
                } else {
                    foreach(string _httpmethod in attr.methods) {
                        if(routes[attr.route].methods.ContainsKey(_httpmethod)) {
                            throw new MethodAlreadyDefinedException(attr.route, _httpmethod);
                        }

                        routes[attr.route].methods.Add(_httpmethod, new RouteInfo() {
                            method = method
                        });
                    }
                }

                //string value = attr.; 
            }
        }

        // Handle requests
        Task listenTask = HandleIncomingConnections();
        listenTask.GetAwaiter().GetResult();

        // Close the listener
        listener.Close();
    }
}