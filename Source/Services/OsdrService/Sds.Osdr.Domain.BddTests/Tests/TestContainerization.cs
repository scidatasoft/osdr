using System;
using MongoDB.Bson;

namespace Sds.Osdr.BddTests
{
    public class VariablesRepository
    {
        private BsonDocument _variables;
        
        public VariablesRepository()
        {
            _variables = new BsonDocument();
        }

        public void Push(string name, object value)
        {
            var bsonValue = BsonValue.Create(value);
            
            _variables.Add(name, bsonValue);
        }

        public BsonValue Get(string name)
        {
            return _variables[name];
        }
    }
    
    public class ContainerTest
    {
        private bool _isInitialized;
        public VariablesRepository Enviroment { get; set; }

        public ContainerTest()
        {
            _isInitialized = false;
            Enviroment = new VariablesRepository();
        }

        public void SetTest(IInitializerTest initializerTest)
        {
            if (!_isInitialized)
            {
                initializerTest?.Initialize(Enviroment);

                _isInitialized = true;
            }
        }
    }

    public interface IInitializerTest
    {
        void Initialize(VariablesRepository env);
    }
}