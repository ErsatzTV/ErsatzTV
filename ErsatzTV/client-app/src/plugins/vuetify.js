import Vue from 'vue';
import Vuetify from 'vuetify/lib/framework';

Vue.use(Vuetify);

export default new Vuetify({
  icons: {
      iconfont: "mdi", // default - only for display purposes
  },
  theme: {
      themes: {
          dark: {
              primary: "#8f63ff",
              secondary: "#673ab7",
              accent: "#03a9f4",
              error: "#f44336",
              warning: "#ffc107",
              info: "#00bcd4",
              success: "#4caf50",
          },
      },
      options: {
          customProperties: true,
      },
      dark: true,
  },
});
