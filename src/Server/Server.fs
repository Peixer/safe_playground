module Server

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Saturn

open LiteDB.FSharp
open LiteDB

open Shared

type Storage() =
    let database =
        let mapper = FSharpBsonMapper()
        let connStr = "Filename=Todo.db;mode=Exclusive"
        new LiteDatabase (connStr, mapper)
    let todos = database.GetCollection<Todo> "todos"

    member _.GetTodos () =
        todos.FindAll () |> List.ofSeq
    member _.AddTodo (todo:Todo) =
        if Todo.isValid todo.Description then
            todos.Insert todo |> ignore
            Ok ()
        else
            Error "Invalid todo"

let storage = Storage()

if storage.GetTodos() |> Seq.isEmpty then
    storage.AddTodo(Todo.create "Create new SAFE project") |> ignore
    storage.AddTodo(Todo.create "Write your app222") |> ignore
    storage.AddTodo(Todo.create "Ship it !!!") |> ignore

let todosApi =
    { getTodos = fun () -> async { return storage.GetTodos() }
      addTodo =
          fun todo ->
              async {
                  match storage.AddTodo todo with
                  | Ok () -> return todo
                  | Error e -> return failwith e
              } }

let webApp =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue todosApi
    |> Remoting.buildHttpHandler

let app =
    application {
        url "http://0.0.0.0:8085"
        use_router webApp
        memory_cache
        use_static "public"
        use_gzip
    }

run app
