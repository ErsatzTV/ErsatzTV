import Vue from 'vue'
import App from './App.vue'
import vuetify from './plugins/vuetify'
import router from './router'
import 'roboto-fontface/css/roboto/roboto-fontface.css'
import '@mdi/font/css/materialdesignicons.css'
import autoPageTitleMixin from './mixins/autoPageTitle'

Vue.config.productionTip = false

// Mixin to automate the page title when navigating... Will default to "ErsatzTV2 if no title value exported from page.
Vue.mixin(autoPageTitleMixin)

new Vue({
  vuetify, router,
  render: h => h(App)
}).$mount('#app')
