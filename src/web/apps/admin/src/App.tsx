import { APP_NAME } from '@fms/types';
import { Button } from '@fms/ui';
import './index.css';

function App() {
  return (
    <main>
      <h1>{APP_NAME} Admin</h1>
      <p>Form configuration portal.</p>
      <Button>Create form</Button>
    </main>
  );
}

export default App;
