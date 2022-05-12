using System.Collections.Generic;

namespace MadelineParty {
    static class Extensions {

        public static T OrDefault<S, T>(this Dictionary<S, T> self, S key, T defaultValue) {
            if(self.TryGetValue(key, out T value)) {
                return value;
            }
            return defaultValue;
        }

        public static T OrDefault<S, T>(this Dictionary<S, object> self, S key, T defaultValue) {
            if (self.TryGetValue(key, out object value)) {
                return (T)value;
            }
            return defaultValue;
        }

    }
}
