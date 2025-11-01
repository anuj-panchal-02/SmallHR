import { Result, Button } from 'antd';
import { useNavigate } from 'react-router-dom';

export default function UnknownModule() {
  const navigate = useNavigate();
  return (
    <Result
      status="info"
      title="Module not available"
      subTitle="This module is not yet implemented."
      extra={<Button type="primary" onClick={() => navigate('/dashboard')}>Back to Dashboard</Button>}
    />
  );
}
