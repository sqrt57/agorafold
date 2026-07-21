import { Route } from '@solidjs/router'
import RequireAuth from './components/RequireAuth'
import BrowsePage from './pages/BrowsePage'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import ListingFormPage from './pages/ListingFormPage'
import ListingDetailPage from './pages/ListingDetailPage'
import MyListingsPage from './pages/MyListingsPage'
import ConversationsInboxPage from './pages/ConversationsInboxPage'
import ConversationThreadPage from './pages/ConversationThreadPage'

export default function App() {
  return (
    <>
      <Route path="/" component={BrowsePage} />
      <Route path="/login" component={LoginPage} />
      <Route path="/register" component={RegisterPage} />
      <Route path="/listings/new" component={() => <RequireAuth><ListingFormPage /></RequireAuth>} />
      <Route path="/listings/mine" component={() => <RequireAuth><MyListingsPage /></RequireAuth>} />
      <Route path="/listings/:id/edit" component={() => <RequireAuth><ListingFormPage /></RequireAuth>} />
      <Route path="/listings/:id" component={ListingDetailPage} />
      <Route path="/conversations" component={() => <RequireAuth><ConversationsInboxPage /></RequireAuth>} />
      <Route path="/conversations/:id" component={() => <RequireAuth><ConversationThreadPage /></RequireAuth>} />
    </>
  )
}
