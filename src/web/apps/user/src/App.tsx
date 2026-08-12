import { FormRenderer } from '@fms/form-renderer';
import { APP_NAME } from '@fms/types';
import './index.css';

function App() {
  return (
    <main>
      <h1>{APP_NAME}</h1>
      <FormRenderer schema={{ title: 'Feedback form', properties: {} }} />
    </main>
  );
}

export default App;
