using System.Collections.Generic;
using System.Linq;

namespace Humphrey.FrontEnd
{
    public class HumphreyParser
    {
        public HumphreyParser(IEnumerable<Result<Tokens>> toParse)
        {
            tokens = new Queue<Result<Tokens>>(toParse);
            NextToken();
        }

        void NextToken()
        {
            if (tokens.Count != 0)
                lookahead = tokens.Dequeue();
            else
                lookahead = new Result<Tokens>();
        }

        Queue<Result<Tokens>> tokens;
        Result<Tokens> lookahead;

        (bool success, string item) Item(Tokens kind)
        {
            if (lookahead.HasValue && lookahead.Value == kind)
            {
                var v = lookahead.ToStringValue();
                if (kind == Tokens.Number)
                    v = HumphreyTokeniser.ConvertNumber(v);
                NextToken();
                return (true, v);
            }
            return (false, "");
        }

        // * (0 or more)
        protected (bool success, string[]) ItemList(Tokens kind)
        {
            var list = new List<string>();
            while (true)
            {
                var (success, item) = Item(kind);
                if (success)
                    list.Add(item);
                else
                    break;
            }

            return (true, list.ToArray());
        }


        // Number
        public (bool success, string) Number => Item(Tokens.Number);

        // Identifier
        public (bool success, string) Identifier => Item(Tokens.Identifier);

        // Number*
        public (bool success, string[]) NumberList => ItemList(Tokens.Number);

        // Identifier*        
        public (bool success, string[]) IdentifierList => ItemList(Tokens.Identifier);


        // Root
        public (bool success, string[]) File => ItemList(Tokens.Identifier);
    }
}
