import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function NavBar() {
  const { user, isAuthenticated, logout } = useAuth()
  const navigate = useNavigate()

  async function handleLogout() {
    await logout()
    navigate('/')
  }

  return (
    <nav className="navbar">
      <Link className="brand" to="/">AgoraFold</Link>
      <div className="links">
        <Link to="/">Browse</Link>
        {isAuthenticated ? (
          <>
            <Link to="/listings/new">Post a listing</Link>
            <Link to="/listings/mine">My listings</Link>
            <Link to="/conversations">Messages</Link>
            <span className="muted">{user?.displayName}</span>
            <button className="secondary" onClick={handleLogout}>Log out</button>
          </>
        ) : (
          <>
            <Link to="/login">Log in</Link>
            <Link to="/register">Register</Link>
          </>
        )}
      </div>
    </nav>
  )
}
