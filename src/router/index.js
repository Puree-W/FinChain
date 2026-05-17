import { createRouter, createWebHistory } from 'vue-router'
import Homepage from '../page/Homepage.vue'
import Configuration from '../page/Configuration.vue'
import LLMConfigPanel from '../components/configuration/LLMConfigPanel.vue'
import ModelSettingPanel from '../components/configuration/ModelSettingPanel.vue'

const routes = [
  {
    path: '/',
    name: 'Home',
    component: Homepage,
  },
  {
    path: '/configuration',
    component: Configuration,
    children: [
      { path: '', redirect: '/configuration/llm' },
      { path: 'llm', name: 'LLMConfiguration', component: LLMConfigPanel },
      { path: 'model', name: 'ModelSetting', component: ModelSettingPanel },
    ],
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

export default router
