# Thermos
A C# framework for building web applications based on the Flask syntax.

## A Simple Hello World Application

Heres a simple thermos application:

```cs 
using thermos;

namespace helloworld;

public class Program {
    [Route("/")]
    public static string HelloWorld() {
        return "<p>Hello, World!</p>";
    }

    static void Main() {
        var app = new Thermos();

        app.Run();
    }
}
```

And then the same application in Flask:

```py
from flask import Flask

app = Flask(__name__)

@app.route("/")
def hello_world():
    return "<p>Hello, World!</p>"
```

Thermos aims to follow the Flask syntax to the best of it's ability 
to allow for an easy transition.

