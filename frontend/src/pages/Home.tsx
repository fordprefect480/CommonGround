import { Link } from 'react-router-dom'
import { useAppConfig } from '../AppConfigContext'

export default function Home() {
  const { gardenName } = useAppConfig()
  return (
    <>
      <header className="app-header">
        <h1 className="app-title">{gardenName}</h1>
        <p className="app-subtitle">Our local community garden</p>
      </header>

      <main className="main-content">
        <section className="hero-section" aria-labelledby="welcome-heading">
          <div className="card">
            <h2 id="welcome-heading" className="section-title">Welcome to the garden</h2>
            <p className="lede">
              {gardenName} is a shared patch of earth tended by neighbours.
              We grow vegetables, herbs and friendships side by side &mdash;
              and there&rsquo;s always room for one more pair of hands.
            </p>
          </div>
        </section>

        <section className="info-section" aria-labelledby="visit-heading">
          <div className="card">
            <h2 id="visit-heading" className="section-title">Come visit</h2>
            <dl className="info-list">
              <div className="info-row">
                <dt>Working bees</dt>
                <dd>Saturdays, 9am &ndash; 11am</dd>
              </div>
              <div className="info-row">
                <dt>Open days</dt>
                <dd>First Sunday of the month</dd>
              </div>
              <div className="info-row">
                <dt>Where</dt>
                <dd>Corner of Main and Park Street</dd>
              </div>
            </dl>
            <p className="card-note">
              Membership, the blog, and our newsletter are coming soon.
            </p>
          </div>
        </section>
      </main>

      <footer className="app-footer">
        <p>
          &copy; {new Date().getFullYear()} {gardenName}
          <span className="footer-sep" aria-hidden="true"> &middot; </span>
          <Link to="/admin" className="footer-link">Admin</Link>
        </p>
      </footer>
    </>
  )
}
