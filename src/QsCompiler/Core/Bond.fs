// contains dummy classes to be replaced
module Microsoft.Quantum.QsCompiler.Bond

    open System

    type SpecializationImplementation () = 

        member public this.Deserialize () : SyntaxTree.SpecializationImplementation = 
            NotImplementedException() |> raise

        static member public Create (deserialized : SyntaxTree.SpecializationImplementation) : SpecializationImplementation = 
            
            NotImplementedException() |> raise
            
