import { Route, Routes } from 'react-router-dom'
import NavBar from './components/NavBar'
import RequireAuth from './components/RequireAuth'
import BrowsePage from './pages/BrowsePage'
import LoginPage from './pages/LoginPage'
import RegisterPage from './pages/RegisterPage'
import ListingFormPage from './pages/ListingFormPage'
import ListingDetailPage from './pages/ListingDetailPage'
import MyListingsPage from './pages/MyListingsPage'
import ConversationsInboxPage from './pages/ConversationsInboxPage'
import ConversationThreadPage from './pages/ConversationThreadPage'

function App() {
  return (
    <>
      <NavBar />
      <main className="container">
        <Routes>
          <Route path="/" element={<BrowsePage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route
            path="/listings/new"
            element={
              <RequireAuth>
                <ListingFormPage />
              </RequireAuth>
            }
          />
          <Route
            path="/listings/mine"
            element={
              <RequireAuth>
                <MyListingsPage />
              </RequireAuth>
            }
          />
          <Route
            path="/listings/:id/edit"
            element={
              <RequireAuth>
                <ListingFormPage />
              </RequireAuth>
            }
          />
          <Route path="/listings/:id" element={<ListingDetailPage />} />
          <Route
            path="/conversations"
            element={
              <RequireAuth>
                <ConversationsInboxPage />
              </RequireAuth>
            }
          />
          <Route
            path="/conversations/:id"
            element={
              <RequireAuth>
                <ConversationThreadPage />
              </RequireAuth>
            }
          />
        </Routes>
      </main>
    </>
  )
}

export default App
