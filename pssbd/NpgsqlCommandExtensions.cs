using Npgsql;

namespace pssbd
{
    public static class NpgsqlCommandExtensions
    {
        public static NpgsqlCommand AddParam(this NpgsqlCommand cmd, string name, object value)
        {
            cmd.Parameters.AddWithValue(name, value);
            return cmd;
        }

        public static NpgsqlCommand AddParams(this NpgsqlCommand cmd, object parameters)
        {
            foreach (var prop in parameters.GetType().GetProperties())
            {
                cmd.Parameters.AddWithValue(prop.Name, prop.GetValue(parameters));
            }
            return cmd;
        }
    }
}